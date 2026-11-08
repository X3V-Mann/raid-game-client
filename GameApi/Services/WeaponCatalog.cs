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
        /// <summary>Меткость (0-100, чем выше тем меньше шанс промаха).</summary>
        public int Accuracy { get; set; }
        /// <summary>Требуемый уровень навыка владения оружием (0 = без ограничений).</summary>
        public int RequiredSkill { get; set; }
    }

    private const string EmbeddedResourceName = "GameApi.Data.weapon_list.json";

    public static List<WeaponDef> All { get; } = Load();

    public static WeaponDef? ByCode(string code) =>
        All.FirstOrDefault(w => string.Equals(w.Code, code, StringComparison.OrdinalIgnoreCase));

    public static WeaponDef? ByName(string name) =>
        All.FirstOrDefault(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Иконки оружия лежат в public/items/weapon. Подбираем по названию/коду,
    /// потому что code в weapon_list.json не задан и выводится из названия.
    /// </summary>
    private const string WeaponIconFolder = "/items/weapon/";

    private static readonly (string Pattern, string File)[] IconRules =
    {
        ("винторез", "vss_vintorez.png"),
        ("vss", "vss_vintorez.png"),
        ("свд", "SVD.png"),
        ("svd", "SVD.png"),
        ("пп-19 «бизон»", "pp19_Bizon.png"),
        ("pp19 bison", "pp19_Bizon.png"),
        ("бизон", "pp19_Bizon.png"),
        ("пп-19", "pp19.png"),
        ("pp-19", "pp19.png"),
        ("pp19", "pp19.png"),
        ("m4a1", "m4a1.png"),
        ("ak-74un", "АКС74У.png"),
        ("ak-74u", "АКС74У.png"),
        ("ak-74у", "АКС74У.png"),
        ("ак-74у", "АКС74У.png"),
        ("ak (1947)", "Ak1947.png"),
        ("ak-47", "Ak1947.png"),
        ("ak-74", "ak74.png"),
        ("ak74", "ak74.png"),
        ("ак-74", "ak74.png"),
        ("ак74", "ak74.png"),
        ("акмс", "AKMS.png"),
        ("akms", "AKMS.png"),
        ("акм", "akm.png"),
        ("akm", "akm.png"),
        ("акс", "Ak1947.png"),
        ("арбалет", "crossbow.png"),
        ("crossbow", "crossbow.png"),
        ("пм", "pm.png"),
        ("6х4", "6х4 Нож.png"),
        ("6x4", "6х4 Нож.png"),
        ("m5 bayonet", "m5bayonet.png"),
        ("штык m5", "m5bayonet.png"),
        ("нож разведчика", "НожРазведчика.png"),
        ("нож спецназа", "НожСпецназа.png"),
        ("охотничий топор", "охотничий_топор.png"),
        ("охотничий нож", "ОхотничийНож.png"),
        ("пожарный топор", "ПожарныйТопор.png"),
        ("топорик", "Топорик.png"),
        ("топор", "axe.png"),
        ("мачете", "machete.png"),
        ("machete", "machete.png"),
        ("нож", "knife.png"),
        ("knife", "knife.png"),
        ("кувалда", "sledge.png"),
        ("sledge", "sledge.png"),
    };

    private static readonly HashSet<string> KnownWeaponFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "6х4 Нож.png", "Ak1947.png", "ak74.png", "akm.png", "AKMS.png", "axe.png", "crossbow.png",
        "knife.png", "m4a1.png", "m5bayonet.png", "machete.png", "pm.png", "pp19.png", "pp19_Bizon.png",
        "sledge.png", "SVD.png", "vss_vintorez.png", "АКС74У.png", "НожРазведчика.png", "НожСпецназа.png",
        "охотничий_топор.png", "ОхотничийНож.png", "ПожарныйТопор.png", "Топорик.png",
    };

    private static string CaliberDisplay(string caliber) =>
        string.Equals(caliber, "bolt", StringComparison.OrdinalIgnoreCase)
            ? "болты для арбалета"
            : caliber;

    private static string WeaponIcon(string file) => WeaponIconFolder + file;

    public static string IconFor(WeaponDef w)
    {
        var name = (w.Name ?? "").ToLowerInvariant();
        var code = (w.Code ?? "").ToLowerInvariant();

        foreach (var (pattern, file) in IconRules)
            if (name.Contains(pattern, StringComparison.OrdinalIgnoreCase) || code.Contains(NormalizeCode(pattern), StringComparison.OrdinalIgnoreCase))
                return WeaponIcon(file);

        // Пробуем прямое совпадение по нормализованному коду/имени с известными файлами.
        var nCode = NormalizeCode(w.Code ?? "");
        var nName = NormalizeCode(w.Name ?? "");
        foreach (var file in KnownWeaponFiles)
        {
            var nf = NormalizeCode(Path.GetFileNameWithoutExtension(file));
            if (nf.Length > 0 && (nCode.Equals(nf, StringComparison.OrdinalIgnoreCase) || nName.Equals(nf, StringComparison.OrdinalIgnoreCase)))
                return WeaponIcon(file);
        }

        return ItemCatalog.MissingIcon;
    }

    /// <summary>Создаёт предмет оружия из определения (для продажи торговцам и лута).</summary>
    public static Item CreateItem(WeaponDef w)
    {
        bool isGrenade = string.Equals(w.WeaponClass, "Grenade", StringComparison.OrdinalIgnoreCase);
        bool isMelee = string.Equals(w.WeaponClass, "Melee", StringComparison.OrdinalIgnoreCase);

        // В weapon_list.json у дробовиков damage указан на одну дробину,
        // а ammoPerShot — количество дробин в выстреле. Показываем и наносим
        // суммарный урон выстрела, чтобы дробовик не был слабее пистолета.
        int projectiles = isGrenade || isMelee ? 1 : Math.Max(1, w.AmmoPerShot);
        int damage = isGrenade
            ? w.Damage
            : isMelee
                ? Math.Clamp(w.Damage, 1, 200)
                : Math.Clamp(w.Damage * projectiles, 1, 200);

        string damageText = projectiles > 1
            ? $"Урон: {damage} ({w.Damage}×{projectiles})."
            : $"Урон: {damage}.";

        var description = new List<string> { $"Оружие класса «{w.WeaponClass}»." };
        if (!isGrenade && !isMelee && !string.IsNullOrWhiteSpace(w.Caliber))
            description.Add($"Калибр: {CaliberDisplay(w.Caliber)}.");
        description.Add(damageText);

        return new Item
        {
            Code = "wpn_" + w.Code,
            Name = w.Name,
            Description = string.Join(" ", description),
            Icon = IconFor(w),
            Type = isGrenade ? ItemType.Misc : isMelee ? ItemType.WeaponMelee : ItemType.WeaponRanged,
            Rarity = RarityFor(w.WeaponClass),
            Price = PriceFor(damage, w.WeaponClass),
            Damage = damage,
            Caliber = isGrenade ? "" : (w.Caliber ?? ""),
            Weight = isMelee ? 2.0 : 3.5,
            Lootable = false,
            IsGrenade = isGrenade,
            RequiredLoyalty = LoyaltyForDamage(damage),
            LootTier = LootTierForDamage(damage),
            Accuracy = w.Accuracy,
            RangeEffective = (int)Math.Clamp(w.RangeEffective, 0, 50),
            RangeMax = (int)Math.Clamp(w.RangeMax, 0, 100),
            Rpm = w.Rpm,
            RequiredSkill = w.RequiredSkill,
        };
    }

    /// <summary>
    /// Обновляет характеристики уже существующего предмета оружия по актуальному
    /// weapon_list.json (нужно для старых сохранений, где урон был «сплющен» до 25).
    /// </summary>
    public static void RefreshItem(Item? item)
    {
        if (item == null || !item.Code.StartsWith("wpn_", StringComparison.OrdinalIgnoreCase))
            return;

        var def = ByCode(item.Code[4..]);
        if (def == null) return;

        var fresh = CreateItem(def);
        item.Name = fresh.Name;
        item.Description = fresh.Description;
        item.Icon = fresh.Icon;
        item.Type = fresh.Type;
        item.Rarity = fresh.Rarity;
        item.Price = fresh.Price;
        item.Damage = fresh.Damage;
        item.Caliber = fresh.Caliber;
        item.Weight = fresh.Weight;
        item.IsGrenade = fresh.IsGrenade;
        item.RequiredLoyalty = fresh.RequiredLoyalty;
        item.LootTier = fresh.LootTier;
        item.Accuracy = fresh.Accuracy;
        item.RangeEffective = fresh.RangeEffective;
        item.RangeMax = fresh.RangeMax;
        item.Rpm = fresh.Rpm;
        item.RequiredSkill = fresh.RequiredSkill;
    }

    /// <summary>Обновляет всё оружие игрока: экипировку, инвентарь и схрон.</summary>
    public static void RefreshPlayerWeapons(Player p)
    {
        RefreshItem(p.WeaponMelee);
        RefreshItem(p.WeaponRanged);
        RefreshItem(p.Suit);
        foreach (var item in p.Inventory) RefreshItem(item);
        foreach (var item in p.Shelter.Stash) RefreshItem(item);
    }

    /// <summary>Сколько патронов тратится за одно нажатие «Атака».</summary>
    public static int AmmoPerShot(Item? weapon)
    {
        if (weapon == null) return 1;
        var code = weapon.Code.StartsWith("wpn_", StringComparison.OrdinalIgnoreCase)
            ? weapon.Code[4..]
            : weapon.Code;
        var def = ByCode(code);
        return Math.Max(1, def?.AmmoPerShot ?? 1);
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

    /// <summary>
    /// Цена оружия: ближний бой 300–1000 ₽, огнестрел 700–250000 ₽.
    /// Формула учитывает урон и класс оружия; лояльность торговца даёт скидку.
    /// </summary>
    public static int PriceFor(int damage, string weaponClass = "")
    {
        if (string.Equals(weaponClass, "Melee", StringComparison.OrdinalIgnoreCase))
        {
            int d = Math.Clamp(damage, 1, 35);
            return Math.Clamp(250 + d * 22, 300, 1000);
        }

        int rd = Math.Clamp(damage, 1, 200);
        double classMultiplier = weaponClass switch
        {
            "Pistol" => 0.88,
            "MachinePistol" => 0.94,
            "PDW" => 1.00,
            "SMG" => 1.02,
            "Shotgun" => 1.06,
            "Crossbow" => 1.05,
            "Rifle" => 1.18,
            "SniperRifle" => 1.50,
            "MachineGun" => 1.38,
            _ => 1.00,
        };

        // Более плавная кривая: меньше одинаковых цен на слабом оружии.
        double raw = 700.0 * Math.Pow(rd / 18.0, 2.6) * classMultiplier;
        // Небольшая ступенька от урона для уникальности вблизи целых значений.
        raw += rd * 7;
        return Math.Clamp((int)Math.Round(raw / 10.0) * 10, 700, 250000);
    }

    /// <summary>Цена патрона за штуку: 10–50 ₽ в зависимости от калибра.</summary>
    public static int AmmoPriceFor(string? caliber) => (caliber ?? "") switch
    {
        "9x18mm" => 10,
        "7.65mm" => 10,
        "9x19mm" => 12,
        "7.62x25mm" => 12,
        "12 Gauge" => 15,
        ".45 ACP" => 18,
        "4.6x30mm" => 20,
        "5.7x28mm" => 20,
        "bolt" => 20,
        "5.45x39mm" => 25,
        "7.62x39mm" => 26,
        "5.56x45mm NATO" => 28,
        "5.8x42mm" => 28,
        ".50 AE" => 35,
        "7.62x51mm NATO" => 38,
        "9x39mm" => 40,
        "7.92x57mm Mauser" => 40,
        "7.62x54mmR" => 42,
        "12.7x55mm" => 48,
        ".338 Lapua Magnum" => 50,
        ".50 BMG" => 50,
        "2mm EC" => 50,
        _ => 25,
    };

    public static Rarity RarityFor(string weaponClass) => weaponClass switch
    {
        "Rifle" => Rarity.Rare,
        "SniperRifle" or "MachineGun" => Rarity.Epic,
        "SMG" or "PDW" or "Shotgun" or "MachinePistol" or "Crossbow" => Rarity.Uncommon,
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
            Accuracy = Int("accuracy"),
            RequiredSkill = Int("requiredSkill"),
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