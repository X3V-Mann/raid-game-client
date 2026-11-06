using System.Text.Json;
using GameApi.Models;

namespace GameApi.Services;

public class WeaponCatalog
{
    public class WeaponDef
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string WeaponClass { get; set; } = "";
        public string? Caliber { get; set; }
        public int MagazineCapacity { get; set; }
        public List<string> AllowedFireModes { get; set; } = new();
        public bool AimedShotAllowed { get; set; }
        public bool BurstAllowed { get; set; }
        public int Damage { get; set; }
        public double RangeEffective { get; set; }
        public double RangeMax { get; set; }
        public int Rpm { get; set; }
        public int RoundsPerBurst { get; set; }
        public int AmmoPerShot { get; set; }
    }

    private const string EmbeddedResourceName = "GameApi.Data.weapon_list.json";

    public static List<WeaponDef> All { get; } = Load();

    public static WeaponDef? ByCode(string code) =>
        All.FirstOrDefault(w => string.Equals(w.Code, code, StringComparison.OrdinalIgnoreCase));

    public static WeaponDef? ByName(string name) =>
        All.FirstOrDefault(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Создаёт предмет оружия из определения (для продажи торговцам и лута).</summary>
    public static Item CreateItem(WeaponDef w)
    {
        bool isGrenade = string.Equals(w.WeaponClass, "Grenade", StringComparison.OrdinalIgnoreCase);
        return new Item
        {
            Code = "wpn_" + w.Code,
            Name = w.Name,
            Description = $"Оружие класса «{w.WeaponClass}». Урон: {w.Damage}.",
            Icon = ItemCatalog.MissingIcon,
            Type = isGrenade ? ItemType.Misc : ItemType.WeaponRanged,
            Rarity = RarityFor(w.WeaponClass),
            Price = PriceFor(w.Damage),
            Damage = w.Damage,
            Caliber = isGrenade ? "" : (w.Caliber ?? ""),
            Weight = string.Equals(w.WeaponClass, "Melee", StringComparison.OrdinalIgnoreCase) ? 2.0 : 3.5,
            Lootable = false,
            IsGrenade = isGrenade,
            RequiredLoyalty = LoyaltyForDamage(w.Damage),
            LootTier = LootTierForDamage(w.Damage),
        };
    }

    public static int LoyaltyForDamage(int damage) => damage switch
    {
        <= 25 => 1,
        <= 30 => 2,
        <= 35 => 3,
        <= 40 => 4,
        <= 45 => 5,
        <= 52 => 6,
        <= 60 => 7,
        <= 70 => 8,
        <= 85 => 9,
        _ => 10,
    };

    public static int LootTierForDamage(int damage) => Math.Clamp((LoyaltyForDamage(damage) + 1) / 2, 1, 7);

    public static int PriceFor(int damage) => damage * 1421;

    public static Rarity RarityFor(string weaponClass) => weaponClass switch
    {
        "Rifle" => Rarity.Rare,
        "SniperRifle" or "MachineGun" => Rarity.Epic,
        "SMG" or "PDW" or "Shotgun" or "MachinePistol" => Rarity.Uncommon,
        _ => Rarity.Common,
    };

    private static string NormalizeCode(string name)
    {
        var chars = new List<char>();
        foreach (var c in name)
            if (char.IsLetterOrDigit(c)) chars.Add(char.ToLowerInvariant(c));
        return new string(chars.ToArray());
    }

    private static WeaponDef ParseOne(JsonElement e)
    {
        List<string> modes = new();
        if (e.TryGetProperty("allowedFireModes", out var modesEl) && modesEl.ValueKind == JsonValueKind.Array)
            foreach (var m in modesEl.EnumerateArray())
                if (m.ValueKind == JsonValueKind.String) modes.Add(m.GetString() ?? "");

        string? Str(string p) =>
            e.TryGetProperty(p, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        int Int(string p) =>
            e.TryGetProperty(p, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i) ? i : 0;

        bool Bool(string p) =>
            e.TryGetProperty(p, out var v) && v.ValueKind == JsonValueKind.True;

        var name = Str("name") ?? "";
        var code = Str("code");
        if (string.IsNullOrWhiteSpace(code)) code = NormalizeCode(name);

        return new WeaponDef
        {
            Code = code,
            Name = name,
            WeaponClass = Str("weaponClass") ?? "",
            Caliber = Str("caliber"),
            MagazineCapacity = Int("magazineCapacity"),
            AllowedFireModes = modes,
            AimedShotAllowed = Bool("aimedShotAllowed"),
            BurstAllowed = Bool("burstAllowed"),
            Damage = Int("damage"),
            RangeEffective = e.TryGetProperty("rangeEffective", out var re) && re.ValueKind == JsonValueKind.Number ? re.GetDouble() : 0,
            RangeMax = e.TryGetProperty("rangeMax", out var rm) && rm.ValueKind == JsonValueKind.Number ? rm.GetDouble() : 0,
            Rpm = Int("rpm"),
            RoundsPerBurst = Int("roundsPerBurst"),
            AmmoPerShot = Int("ammoPerShot"),
        };
    }

    private static List<WeaponDef> Load()
    {
        var result = new List<WeaponDef>();
        try
        {
            var assembly = typeof(WeaponCatalog).Assembly;
            var stream = assembly.GetManifestResourceStream(EmbeddedResourceName);
            if (stream == null) return result;
            using var reader = new StreamReader(stream);
            using var doc = JsonDocument.Parse(reader.ReadToEnd());
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
                foreach (var w in root.EnumerateArray()) result.Add(ParseOne(w));
            else if (root.TryGetProperty("weapons", out var weapons) && weapons.ValueKind == JsonValueKind.Array)
                foreach (var w in weapons.EnumerateArray()) result.Add(ParseOne(w));
        }
        catch
        {
            // Если файл не загрузился — просто без дополнительного оружия.
        }
        return result;
    }
}