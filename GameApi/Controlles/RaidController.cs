using GameApi.Models;
using GameApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameApi.Controllers;

[ApiController]
[Route("api/raid")]
public class RaidController : ControllerBase
{
    private readonly LootService _loot;
    private readonly GameStateService _state;
    private readonly ItemCatalog _catalog;
    private readonly LocationCatalog _locations;
    private readonly Random _rng = new();

    public RaidController(LootService loot, GameStateService state, ItemCatalog catalog, LocationCatalog locations)
        => (_loot, _state, _catalog, _locations) = (loot, state, catalog, locations);

    [HttpGet("locations")]
    public object GetLocations()
    {
        var p = _state.Player;
        return _locations.All.Select(l => new
        {
            l.Id,
            l.Name,
            l.Tier,
            l.Description,
            l.Image,
            l.Icon,
            l.BossChance,
            unlocked = _locations.IsUnlocked(p, l),
            unlockHint = l.UnlockHint,
        }).ToList();
    }

    [HttpPost("start")]
    public ActionResult<List<RaidEncounter>> Start([FromBody] RaidStartDto dto)
    {
        var player = _state.Player;
        if (player.Hp <= 0)
            return BadRequest("Ты без сил. Отдохни в убежище или подлечись.");

        var loc = _locations.Get(dto.LocationId) ?? _locations.All[0];
        if (!_locations.IsUnlocked(player, loc))
            return BadRequest($"Локация закрыта. {loc.UnlockHint}");

        int lvl = player.Level;
        int effectiveTier = Math.Min(loc.Tier, 1 + lvl / 2);

        int totalCells = _rng.Next(5, 21);
        var encounters = new List<RaidEncounter>();

        for (int i = 1; i <= totalCells; i++)
        {
            var enc = new RaidEncounter { Index = i };

            if (i == 1)
            {
                enc.Type = EncounterType.Empty; enc.Title = "Вход"; enc.Description = "Ты входишь в зону."; enc.Icon = "🚪";
                encounters.Add(enc); continue;
            }
            if (i == totalCells)
            {
                enc.Type = EncounterType.Empty; enc.Title = "Выход"; enc.Description = "Знакомые очертания бункера"; enc.Icon = "🚪";
                encounters.Add(enc); continue;
            }

            int combatChance = 20 + i * 3;
            int lootChance = 30;
            int sceneChance = 15;
            int thoughtChance = 10;
            int trapChance = 5 + loc.Tier;

            int roll = _rng.Next(100);

            if (roll < combatChance)
            {
                enc.Type = EncounterType.Combat; enc.Title = "Засада"; enc.Description = "Враги"; enc.Icon = "⚔️";
            }
            else if (roll < combatChance + lootChance)
            {
                enc.Type = EncounterType.Loot; enc.Title = "Тайник"; enc.Description = "Находка"; enc.Icon = "📦";
                var loot = GenerateLocationLoot(loc, effectiveTier);
                enc.LootItems = loot.Items; enc.MoneyReward = loot.Money; enc.XpReward = loot.Xp;
            }
            else if (roll < combatChance + lootChance + sceneChance)
            {
                var scene = RandomScene(loc); enc.Type = EncounterType.Scene; enc.Title = scene.Title; enc.Description = scene.Description; enc.Icon = scene.Icon;
            }
            else if (roll < combatChance + lootChance + sceneChance + thoughtChance)
            {
                enc.Type = EncounterType.Scene; enc.Title = "Мысли"; enc.Description = RandomThought(loc); enc.Icon = "💭";
            }
            else if (roll < combatChance + lootChance + sceneChance + thoughtChance + trapChance)
            {
                enc.Type = EncounterType.Trap; enc.Title = "Ловушка"; enc.Description = "Щелчок под ногами"; enc.Icon = "⚠️";
                enc.Damage = 5 + effectiveTier * 3 + _rng.Next(0, 5);
            }
            else
            {
                enc.Type = EncounterType.Empty; enc.Title = "Тишина"; enc.Description = "Пусто"; enc.Icon = "·";
            }

            // На пустых/сюжетных клетках тоже можно найти небольшой тайник.
            if ((enc.Type == EncounterType.Empty || enc.Type == EncounterType.Scene)
                && _rng.NextDouble() < 0.28)
            {
                var found = GenerateLocationLoot(loc, effectiveTier);
                enc.LootItems = found.Items;
                enc.MoneyReward = found.Money;
                enc.XpReward = found.Xp;
            }

            encounters.Add(enc);
        }
        return encounters;
    }

    [HttpPost("combat/start")]
    public ActionResult<CombatStateDto> CombatStart([FromBody] CombatStartDto dto)
    {
        var p = _state.Player;
        if (p.Hp <= 0) return BadRequest("Ты без сил, чтобы драться.");

        var loc = _locations.Get(dto.LocationId) ?? _locations.All[0];
        if (!_locations.IsUnlocked(p, loc))
            return BadRequest($"Локация закрыта. {loc.UnlockHint}");

        int t = Math.Min(loc.Tier, 1 + p.Level / 2);

        // До 10-го уровня — только 1–2 противника, дальше группы могут быть больше.
        int maxGroup = p.Level <= 10 ? 2 : 4;
        int groupSize = _rng.Next(1, maxGroup + 1);
        var enemies = new List<Enemy>();
        for (int i = 0; i < groupSize; i++)
            enemies.Add(GenerateEnemy(loc, t, isBoss: i == 0 && _rng.NextDouble() < loc.BossChance));

        _state.CurrentCombat = new CombatStateDto
        {
            Enemies = enemies, PlayerMaxHp = p.MaxHp, PlayerHp = p.Hp,
            Tier = t,
            Log = new List<string> { $"⚔️ Бой: {enemies.Count} противник(а)" }, Finished = false,
        };
        RefreshCombatView(p, _state.CurrentCombat);
        return _state.CurrentCombat;
    }

    public record CombatStartDto(string LocationId);
    public record CombatActionDto(string Action, string? EnemyId = null, string? ItemId = null);

    [HttpPost("combat/action")]
    public RaidActionResult CombatAction([FromBody] CombatActionDto dto)
    {
        var p = _state.Player;
        var state = _state.CurrentCombat;

        // Бой уже завершён (или его не было) — сообщаем об этом клиенту, а не подвешиваем UI.
        if (state == null || state.Finished)
            return new RaidActionResult { State = new CombatStateDto { Finished = true }, Player = p };

        var log = state.Log;
        int baseDamage = 5 + (p.Weapon?.Damage ?? 0) + PerkEffects.DamageBonus(p);
        int playerDefense = (p.Helmet?.Defense ?? 0) + (p.Armor?.Defense ?? 0) + PerkEffects.DefenseBonus(p);
        int levelsGained = 0;

        // Усталость, голод, жажда и перевес напрямую влияют на урон.
        double fatigue = 1.0;
        if (p.Stamina <= 0) fatigue *= 0.50;
        else if (p.Stamina < 15) fatigue *= 0.65;
        else if (p.Stamina < 35) fatigue *= 0.85;
        if (p.Hunger < 20) fatigue *= 0.85;
        if (p.Thirst < 20) fatigue *= 0.85;
        if (p.Overweight) fatigue *= 0.80;

        int playerDamage = Math.Max(1, (int)Math.Round(baseDamage * fatigue));
        int attackCost = 8 + (p.Overweight ? 4 : 0);

        if (p.Hunger == 0 || p.Thirst == 0)
        {
            state.PlayerHp = Math.Max(0, state.PlayerHp - 2);
            log.Add("💀 Голод и жажда: −2 HP");
        }

        switch (dto.Action)
        {
            case "attack":
            case "aim":
            case "burst":
            case "rage":
                {
                    // Боеприпасы: оружие стреляет только если есть нужный патрон в инвентаре.
                    if ((dto.Action == "attack" || dto.Action == "aim" || dto.Action == "burst" || dto.Action == "rage") && p.WeaponRanged != null)
                    {
                        bool hasAmmo = p.Inventory.Any(i => i.Type == ItemType.Ammo && i.Caliber == p.WeaponRanged.Caliber);
                        if (!hasAmmo)
                        {
                            log.Add($"❌ Нет патронов под {p.WeaponRanged.Name} ({p.WeaponRanged.Caliber})");
                            break;
                        }
                    }

                    var target = state.Enemies.FirstOrDefault(e => e.Hp > 0 && e.Id == dto.EnemyId)
                                 ?? state.Enemies.FirstOrDefault(e => e.Hp > 0);
                    if (target == null) break;

                    bool isAim = dto.Action == "aim";
                    bool isBurst = dto.Action == "burst";
                    int cost = isAim ? 18 : isBurst ? 25 : attackCost;

                    if (p.Stamina < cost)
                    {
                        log.Add($"😮‍💨 Не хватает сил{(isAim ? " на прицельный выстрел" : isBurst ? " на очередь" : "")}");
                        p.Stamina = 0;
                        break;
                    }
                    p.Stamina -= cost;

                    int shots = isBurst ? 3 : 1;
                    for (int shot = 0; shot < shots; shot++)
                    {
                        if (target == null || target.Hp <= 0)
                        {
                            target = state.Enemies.FirstOrDefault(e => e.Hp > 0 && e.Id == dto.EnemyId)
                                     ?? state.Enemies.FirstOrDefault(e => e.Hp > 0);
                        }
                        if (target == null) break;

                        double missChance = isBurst ? 0.22 : isAim ? 0.05
                            : (p.Weapon?.Type == ItemType.WeaponRanged ? 0.10 : 0.15);
                        if (fatigue < 0.7) missChance += 0.08;

                        if (_rng.NextDouble() < missChance) { log.Add($"💨 {target.Name}: промах!"); continue; }

                        // Списываем патрон за выстрел (патроны — предметы в инвентаре).
                        var ammoItem = p.Inventory.FirstOrDefault(i => i.Type == ItemType.Ammo && i.Caliber == p.WeaponRanged?.Caliber);
                        if (ammoItem != null) InventoryHelper.RemoveFromInventory(p, ammoItem.Id, 1);

                        int dmg = isAim ? (int)(playerDamage * 1.8) : isBurst ? (int)(playerDamage * 0.55) : playerDamage;
                        if (dto.Action == "rage") dmg = (int)(dmg * 2); // «Ярость» — ускоренный удар
                        dmg = Math.Max(1, dmg - target.Defense);
                        if (!isBurst && _rng.NextDouble() < 0.1) { dmg = (int)(dmg * 1.7); log.Add("💥 КРИТ!"); }

                        target.Hp = Math.Max(0, target.Hp - dmg);
                        log.Add($"⚔️ {target.Name}: -{dmg}");
                        if (target.Hp <= 0) log.Add($"☠️ {target.Name} повержен");
                    }

                    if (isAim) log.Add("🎯 Прицельный выстрел");
                    if (isBurst) log.Add("🔥 Очередь");
                    break;
                }
            case "defend":
                state.IsDefending = true;
                p.Stamina = Math.Min(p.MaxStamina, p.Stamina + 8);
                if (PerkEffects.CoverDefense(p) > 0) state.CoverTurns = Math.Max(state.CoverTurns, 3);
                log.Add("🛡 Ты в обороне — урон снижен вдвое, +8 выносливости");
                break;
            case "flee":
                {
                    int fleeCost = 10;
                    if (p.Stamina < fleeCost)
                    {
                        log.Add("😮‍💨 Нет сил бежать");
                        break;
                    }
                    p.Stamina -= fleeCost;
                    double chance = 0.5 - (p.Overweight ? 0.15 : 0) - (p.Stamina < 15 ? 0.15 : 0);
                    if (_rng.NextDouble() < chance)
                    {
                        state.Finished = true; state.PlayerFled = true; log.Add("🏃 Сбежал");
                        p.Hp = state.PlayerHp;
                        p.Stats.Flees += 1;
                        RefreshCombatView(p, state);
                        _state.CurrentCombat = null;
                        AchievementCatalog.Evaluate(_state);
                        return new RaidActionResult { State = state, Player = p };
                    }
                    log.Add("❌ Не удалось сбежать");
                    break;
                }
            case "item":
                {
                    var item = !string.IsNullOrEmpty(dto.ItemId)
                        ? p.Inventory.FirstOrDefault(i => i.Id == dto.ItemId) ?? p.Inventory.FirstOrDefault(i => i.Code == dto.ItemId)
                        : p.Inventory.FirstOrDefault(i => i.Restores == "hp");

                    if (item == null) { log.Add("❌ Предмет не найден"); break; }

                    if (item.Restores == "hp")
                    {
                        if (state.PlayerHp >= p.MaxHp) { log.Add("❤️ Здоровье уже полное"); break; }

                        int heal = Math.Min(
                            Math.Max(1, (int)Math.Round(item.Restore * PerkEffects.HealMultiplier(p))),
                            p.MaxHp - state.PlayerHp);
                        state.PlayerHp += heal;
                        p.Hp = state.PlayerHp;
                        p.Stats.HpHealed += heal;
                        InventoryHelper.RemoveFromInventory(p, item.Id, 1);
                        log.Add($"🩹 {item.Name}: +{heal} HP");
                    }
                    else if (item.Restores == "hunger")
                    {
                        p.Hunger = Math.Min(100, p.Hunger + item.Restore);
                        p.Stats.FoodEaten += 1;
                        InventoryHelper.RemoveFromInventory(p, item.Id, 1);
                        log.Add($"🍖 {item.Name}: +{item.Restore} голода");
                    }
                    else if (item.Restores == "thirst")
                    {
                        p.Thirst = Math.Min(100, p.Thirst + item.Restore);
                        InventoryHelper.RemoveFromInventory(p, item.Id, 1);
                        log.Add($"💧 {item.Name}: +{item.Restore} жажды");
                    }
                    else { log.Add("❌ В бою этот предмет использовать нельзя"); }
                    break;
                }
            case "grenade":
                {
                    var grenade = !string.IsNullOrEmpty(dto.ItemId)
                        ? p.Inventory.FirstOrDefault(i => i.Id == dto.ItemId) ?? p.Inventory.FirstOrDefault(i => i.Code == dto.ItemId)
                        : p.Inventory.FirstOrDefault(i => i.IsGrenade);
                    if (grenade == null) { log.Add("❌ Нет гранат"); break; }

                    var gt = state.Enemies.FirstOrDefault(e => e.Hp > 0 && e.Id == dto.EnemyId)
                           ?? state.Enemies.FirstOrDefault(e => e.Hp > 0);
                    if (gt == null) break;

                    int blastDmg = Math.Max(15, 25 + state.Tier * 8);
                    gt.Hp = Math.Max(0, gt.Hp - blastDmg);
                    InventoryHelper.RemoveFromInventory(p, grenade.Id, 1);
                    log.Add($"💣 {grenade.Name}: -{blastDmg}");
                    break;
                }
            default:
                log.Add("❓ Неизвестное действие");
                break;
        }

        if (state.Enemies.All(e => e.Hp <= 0))
        {
            state.Finished = true;
            state.PlayerWon = true;
            log.Add("🏆 Победа!");
            p.Hp = state.PlayerHp;

            int killedCount = state.Enemies.Count(e => e.Hp <= 0);
            int bossKills = state.Enemies.Count(e => e.Hp <= 0 && e.IsBoss);
            int normalKills = killedCount - bossKills;
            p.Stats.EnemiesKilled += killedCount;

            foreach (var q in _state.Quests.Concat(_state.StoryQuests)
                         .Where(q => q.IsAccepted && !q.IsCompleted && q.Type == QuestType.Kill))
            {
                if (q.TargetBoss) q.Progress += bossKills;
                else q.Progress += normalKills;
            }

            var loot = _loot.Generate(state.Tier, killedCount, bossKills > 0, PerkEffects.ExtraLootRolls(p));
            RefreshCombatView(p, state);
            _state.CurrentCombat = null;
            return new RaidActionResult
            {
                State = state,
                DroppedLoot = loot.Items,
                DroppedMoney = loot.Money,
                DroppedXp = loot.Xp,
                Player = p,
                LevelsGained = levelsGained,
            };
        }

        // --- Ход врагов ---
        int effectiveDefense = Math.Max(0, playerDefense - (p.Stamina < 10 ? 2 : 0));
        var alive = state.Enemies.Where(e => e.Hp > 0).ToList();
        foreach (var e in alive)
        {
            int effDef = effectiveDefense;
            if (state.CoverTurns > 0)
            {
                // Перк «Укрытие» — укрытие даёт дополнительную защиту на ход.
                effDef += PerkEffects.CoverDefense(p);
                state.CoverTurns = Math.Max(0, state.CoverTurns - 1);
            }

            int enemyDmg = Math.Max(1, e.Damage + (e.Ranged ? 2 : 0) - effDef);
            if (state.IsDefending) enemyDmg = Math.Max(1, enemyDmg / 2);

            int hpBefore = state.PlayerHp;
            double dodgeChance = PerkEffects.DodgeChance(p);
            bool dodged = dodgeChance > 0 && _rng.NextDouble() < dodgeChance;
            int dmgDealt = dodged ? 0 : enemyDmg;

            state.PlayerHp -= dmgDealt;
            p.Stats.HpDamaged += Math.Min(hpBefore, dmgDealt);
            log.Add(dodged ? $"💨 {e.Name}: уклонение!" : $"💥 {e.Name}: -{dmgDealt} HP");
            if (state.PlayerHp <= 0) break;
        }
        state.IsDefending = false;

        p.Hunger = Math.Max(0, p.Hunger - 1);
        p.Thirst = Math.Max(0, p.Thirst - 1);

        if (state.PlayerHp <= 0)
        {
            state.PlayerHp = 0; state.Finished = true; state.PlayerWon = false;
            log.Add("☠️ Ты погиб.");
            p.Hp = 0;
            RefreshCombatView(p, state);
            _state.CurrentCombat = null;
        }
        else
        {
            p.Hp = state.PlayerHp;
            RefreshCombatView(p, state);
        }

        state.Log = log.TakeLast(20).ToList();
        _state.CurrentCombat = state;
        return new RaidActionResult { State = state, Player = p };
    }

    private static void RefreshCombatView(Player p, CombatStateDto s)
    {
        s.PlayerHp = Math.Max(0, s.PlayerHp);
        s.PlayerMaxHp = p.MaxHp;
        s.PlayerStamina = p.Stamina;
        s.PlayerMaxStamina = p.MaxStamina;
        s.PlayerHunger = p.Hunger;
        s.PlayerThirst = p.Thirst;
        s.PlayerWeight = p.CurrentWeight;
        s.PlayerMaxWeight = p.MaxWeight;
        s.PlayerDefense = (p.Helmet?.Defense ?? 0) + (p.Armor?.Defense ?? 0) + PerkEffects.DefenseBonus(p);
        s.PlayerDamage = 5 + (p.Weapon?.Damage ?? 0) + PerkEffects.DamageBonus(p);
    }

    private Enemy GenerateEnemy(Location loc, int t, bool isBoss = false)
    {
        double levelScale = 1.0 + Math.Max(0, _state.Player.Level - 1) * 0.045;

        if (isBoss)
        {
            int hp = (int)Math.Round((60 + t * 30 + _rng.Next(0, 20)) * levelScale);
            return new Enemy
            {
                Name = "БОСС", Icon = "/enemies/boss.png", Hp = hp, MaxHp = hp,
                Damage = Math.Max(1, (int)Math.Round((6 + t * 3) * levelScale)),
                Defense = 1 + t, IsBoss = true,
            };
        }

        var pool = loc.Enemies;
        if (pool.Count == 0) pool = new List<EnemyTemplate> { new() { Name = "Враг", Icon = "/enemies/bandit.png" } };

        double total = pool.Sum(e => e.Weight);
        double roll = _rng.NextDouble() * total;
        double acc = 0;
        EnemyTemplate chosen = pool[0];
        foreach (var e in pool) { acc += e.Weight; if (roll <= acc) { chosen = e; break; } }

        int hpN = (int)Math.Round((chosen.BaseHp + (t - 1) * 10 + _rng.Next(0, 8)) * levelScale);
        int dmg = Math.Max(1, (int)Math.Round((chosen.BaseDamage + (t - 1) * 2 + _rng.Next(0, 3)) * levelScale));

        return new Enemy
        {
            Name = chosen.Name, Icon = chosen.Icon, Hp = hpN, MaxHp = hpN, Damage = dmg,
            Defense = chosen.BaseDefense + Math.Max(0, t - 1), Ranged = chosen.Ranged,
        };
    }

    private (string Title, string Description, string Icon) RandomScene(Location loc)
    {
        if (loc.ScenePool.Count == 0) return ("Тишина", "Ничего", "·");
        var raw = loc.ScenePool[_rng.Next(loc.ScenePool.Count)].Split('|');
        if (raw.Length < 3) return ("Сцена", loc.ScenePool[0], "·");
        return (raw[0], raw[1], raw[2]);
    }

    private string RandomThought(Location loc) => loc.ThoughtPool.Count == 0 ? "…" : loc.ThoughtPool[_rng.Next(loc.ThoughtPool.Count)];

    private LootResult GenerateLocationLoot(Location loc, int tier)
    {
        var result = new LootResult();
        int rolls = 1 + _rng.Next(0, 2);
        for (int i = 0; i < rolls; i++)
        {
            var rarity = RollRarity(loc.RarityWeights);
            var pool = _catalog.All
                .Where(t => t.Lootable && t.LootTier <= tier && t.LootTier >= Math.Max(1, tier - 2) && t.Rarity == rarity)
                .ToList();
            if (pool.Count == 0) pool = _catalog.All.Where(t => t.Lootable && t.LootTier <= tier).ToList();
            result.Items.Add(_catalog.Create(pool[_rng.Next(pool.Count)].Code));
        }
        result.Money = _rng.Next(5, 25) + tier * 5;
        result.Xp = tier * 3;

        var cassette = _loot.RollCassette();
        if (cassette != null) result.Items.Add(cassette);

        return result;
    }

    private Rarity RollRarity(Dictionary<Rarity, double> weights)
    {
        double total = weights.Values.Sum();
        if (total <= 0) return Rarity.Common;

        double roll = _rng.NextDouble() * total;
        double acc = 0;
        foreach (var kv in weights.OrderBy(k => k.Key)) { acc += kv.Value; if (roll <= acc) return kv.Key; }
        return Rarity.Common;
    }
}
