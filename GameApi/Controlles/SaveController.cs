using System.Text.Json;
using GameApi.Models;
using GameApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameApi.Controllers;

[ApiController]
[Route("api/saves")]
public class SaveController : ControllerBase
{
    private readonly GameStateService _state;
    private readonly string _saveDir;

    public SaveController(GameStateService state, IWebHostEnvironment env)
    {
        _state = state;
        _saveDir = Path.Combine(env.ContentRootPath, "saves");
        Directory.CreateDirectory(_saveDir);
    }

    private static readonly JsonSerializerOptions SaveJson = new() { WriteIndented = true };

    public static void LoadLastSave(GameStateService state, string saveDir)
    {
        Directory.CreateDirectory(saveDir);
        state.SaveDirectory = saveDir;
        var autoPath = Path.Combine(saveDir, "autosave.json");
        if (!System.IO.File.Exists(autoPath)) return;

        try
        {
            var parsed = ParseSave(System.IO.File.ReadAllText(autoPath));
            if (parsed.Player != null) ApplyPlayer(state.Player, parsed.Player);
            if (parsed.Version == "4")
            {
                ApplyQuests(state, parsed.Quests, parsed.StoryQuests);
                state.Raid = parsed.Raid is { Finished: false } ? parsed.Raid : null;
                state.CurrentCombat = state.Raid?.Combat;
            }
            else
            {
                // Старый сейв: контент заданий переработан, поэтому берём новый.
                state.CurrentCombat = null;
                state.PendingEnemy = null;
                state.Raid = null;
                state.Player.GameCompleted = false;
                state.Player.Freeplay = false;
                state.ResetQuests();
            }
            Console.WriteLine("Автосохранение успешно загружено.");
        }
        catch (Exception e)
        {
            Console.WriteLine("Ошибка загрузки автосейва: " + e.Message);
        }
    }

    /// <summary>Читает и новый формат (SaveFile), и старый (просто Player).</summary>
    private static (Player? Player, List<Quest>? Quests, List<Quest>? StoryQuests, RaidState? Raid, string? Version) ParseSave(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        bool isWrapped = root.ValueKind == JsonValueKind.Object &&
            (root.TryGetProperty("Player", out _) || root.TryGetProperty("player", out _));

        if (!isWrapped)
            return (JsonSerializer.Deserialize<Player>(json), null, null, null, null);

        var save = JsonSerializer.Deserialize<SaveFile>(json);
        return (save?.Player, save?.Quests, save?.StoryQuests, save?.Raid, save?.Version);
    }

    private static void ApplyPlayer(Player target, Player source)
    {
        target.Xp = source.Xp;
        target.Money = source.Money;
        target.Hp = source.Hp; target.Stamina = source.Stamina;
        target.Hunger = source.Hunger; target.Thirst = source.Thirst;
        target.Name = source.Name; target.Avatar = source.Avatar;
        target.Onboarded = source.Onboarded;
        target.GameCompleted = source.GameCompleted;
        target.Freeplay = source.Freeplay;
        target.PerkPoints = source.PerkPoints;
        target.Perks = source.Perks ?? new();
        target.TraderRep = source.TraderRep ?? new();
        target.Cassettes = source.Cassettes ?? new();
        target.UnlockedLocations = source.UnlockedLocations is { Count: > 0 }
            ? source.UnlockedLocations
            : new List<string> { "city" };

        target.WeaponMelee = source.WeaponMelee; target.WeaponRanged = source.WeaponRanged;
        target.Helmet = source.Helmet; target.Armor = source.Armor; target.Suit = source.Suit; target.Backpack = source.Backpack;

        target.Inventory = source.Inventory ?? new();
        target.Shelter = source.Shelter ?? new ShelterState();
        target.Stats = source.Stats ?? new PlayerStats();

        // Миграция старых сохранений: актуальный урон оружия и коллекция кассет.
        WeaponCatalog.RefreshPlayerWeapons(target);
        InventoryHelper.SyncCassettes(target);
        InventoryHelper.SyncLegacyNames(target);

        // Уровень берём из опыта, чтобы загрузка не выдавала лишние очки навыков.
        target.Level = PlayerProgression.LevelFromXp(target.Xp);
        PlayerProgression.Apply(target, grantPerkPoints: false);

        target.Achievements = source.Achievements ?? new();
        // Бой не должен «переезжать» из загруженного сейва в текущую сессию.
        // (сбрасывается вызывающей стороной через state)
    }

    private static void ApplyQuests(GameStateService state, List<Quest>? quests, List<Quest>? story)
    {
        if (quests != null)
        {
            state.Quests.Clear();
            state.Quests.AddRange(quests);
        }
        if (story != null)
        {
            state.StoryQuests.Clear();
            state.StoryQuests.AddRange(story);
        }
        state.CurrentCombat = null;
        state.PendingEnemy = null;
    }

    private static bool IsSafeFileName(string? fileName) =>
        !string.IsNullOrWhiteSpace(fileName)
        && fileName == Path.GetFileName(fileName)
        && fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
        && fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;

    private string? Resolve(string fileName) =>
        IsSafeFileName(fileName) ? Path.Combine(_saveDir, fileName) : null;

    [HttpGet("list")]
    public IActionResult GetSaves()
    {
        if (!Directory.Exists(_saveDir)) return Ok(new List<object>());

        var files = Directory.GetFiles(_saveDir, "*.json")
            .Select(f =>
            {
                var info = new FileInfo(f);
                var name = Path.GetFileName(f);
                Player? player = null;
                int storyDone = 0;
                int storyTotal = 0;
                try
                {
                    var parsed = ParseSave(System.IO.File.ReadAllText(f));
                    player = parsed.Player;
                    storyDone = parsed.StoryQuests?.Count(q => q.IsCompleted) ?? 0;
                    storyTotal = parsed.StoryQuests?.Count ?? 0;
                }
                catch
                {
                    // Битый сейв не должен ломать список.
                }

                return new
                {
                    name,
                    time = info.LastWriteTime.ToString("dd.MM.yyyy HH:mm:ss"),
                    timestamp = info.LastWriteTimeUtc.Ticks,
                    playerName = player?.Name ?? "",
                    level = player == null ? 0 : PlayerProgression.LevelFromXp(player.Xp),
                    onboarded = player?.Onboarded ?? false,
                    storyDone,
                    storyTotal,
                };
            })
            .OrderByDescending(x => x.timestamp)
            .ToList();

        return Ok(files);
    }

    [HttpPost("save/{slot}")]
    public IActionResult Save(string slot)
    {
        string fileName;
        if (slot == "auto")
        {
            fileName = "autosave.json";
        }
        else if (int.TryParse(slot, out var n) && n is >= 1 and <= 9)
        {
            fileName = $"save_{n}.json";
        }
        else
        {
            return BadRequest("Неверный слот сохранения");
        }

        var path = Path.Combine(_saveDir, fileName);
        var snapshot = new SaveFile
        {
            Player = _state.Player,
            Quests = _state.Quests,
            StoryQuests = _state.StoryQuests,
            Raid = _state.Raid,
            Version = "4",
        };
        var json = JsonSerializer.Serialize(snapshot, SaveJson);
        System.IO.File.WriteAllText(path, json);

        return Ok(new { message = $"Сохранено в {fileName}" });
    }

    [HttpPost("load/{filename}")]
    public IActionResult Load(string filename)
    {
        var path = Resolve(filename);
        if (path == null) return BadRequest("Неверное имя файла");
        if (!System.IO.File.Exists(path)) return NotFound("Сохранение не найдено");

        try
        {
            var parsed = ParseSave(System.IO.File.ReadAllText(path));
            if (parsed.Player == null) return BadRequest("Ошибка чтения");

            ApplyPlayer(_state.Player, parsed.Player);
            if (parsed.Version == "4")
            {
                ApplyQuests(_state, parsed.Quests, parsed.StoryQuests);
                _state.Raid = parsed.Raid is { Finished: false } ? parsed.Raid : null;
                _state.CurrentCombat = _state.Raid?.Combat;
            }
            else
            {
                _state.Raid = null;
                _state.CurrentCombat = null;
                _state.PendingEnemy = null;
                _state.Player.GameCompleted = false;
                _state.Player.Freeplay = false;
                _state.ResetQuests();
            }
            return Ok(_state.Player);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpDelete("delete/{filename}")]
    public IActionResult Delete(string filename)
    {
        var path = Resolve(filename);
        if (path == null) return BadRequest("Неверное имя файла");
        if (!System.IO.File.Exists(path)) return NotFound("Сохранение не найдено");

        System.IO.File.Delete(path);
        return Ok(new { message = "Сохранение удалено" });
    }
}
