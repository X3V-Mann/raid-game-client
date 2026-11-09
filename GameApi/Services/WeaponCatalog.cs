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

    public static List<WeaponDef> All { get; } = new();
    public static List<WeaponDef> TraderWeapons { get; } = new();

    static WeaponCatalog()
    {
        Load();
    }

    public static WeaponDef? ByCode(string code) =>
        All.Concat(TraderWeapons).FirstOrDefault(w => string.Equals(w.Code, code, StringComparison.OrdinalIgnoreCase));

    public static WeaponDef? ByName(string name) =>
        All.Concat(TraderWeapons).FirstOrDefault(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase));

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
        ("пп-19-01", "pp19.png"),
        ("пп-19", "pp19.png"),
        ("pp-19-01", "pp19.png"),
        ("pp-19", "pp19.png"),
        ("pp19", "pp19.png"),
        ("пп-2000", "PP-2000.png"),
        ("pp-2000", "PP-2000.png"),
        ("pp2000", "PP-2000.png"),
        ("ппш-41", "ППШ-41.png"),
        ("ppsh-41", "ППШ-41.png"),
        ("ppsh41", "ППШ-41.png"),
        ("rpk-74", "RPK_74_SAW.png"),
        ("rpk74", "RPK_74_SAW.png"),
        ("rpk", "RPK_SAW.png"),
        ("rpd", "RPD.png"),
        ("fn m249", "m249saw.png"),
        ("m249", "m249saw.png"),
        ("qjy-88", "QJY-88.png"),
        ("qjy88", "QJY-88.png"),
        ("saco m60", "M60.png"),
        ("m60", "M60.png"),
        ("pkp", "PKP_Pecheneg.png"),
        ("pecheneg", "PKP_Pecheneg.png"),
        ("печенег", "PKP_Pecheneg.png"),
        ("pkm", "PKM.png"),
        ("пкм", "PKM.png"),
        ("fn p90", "p90.png"),
        ("p90", "p90.png"),
        ("mp-7", "mp7.png"),
        ("mp7", "mp7.png"),
        ("mp5sd6", "mp5pg6.png"),
        ("mp5sd", "mp5pg6.png"),
        ("mp5a3", "HK_MP5.png"),
        ("mp5", "HK_MP5.png"),
        ("ump45", "ump45.png"),
        ("ump-45", "ump45.png"),
        ("skorpion", "skorpion.png"),
        ("škorpion", "skorpion.png"),
        ("оц-33", "ОЦ-33_Пернач.png"),
        ("пернач", "ОЦ-33_Пернач.png"),
        ("ots-33", "ОЦ-33_Пернач.png"),
        ("апс", "АПС.png"),
        ("aps", "АПС.png"),
        ("aek-971", "AEK_971.png"),
        ("aek971", "AEK_971.png"),
        ("aek 971", "AEK_971.png"),
        ("ан-94", "абакан.png"),
        ("абакан", "абакан.png"),
        ("an-94", "абакан.png"),
        ("9а-91", "9А-91.png"),
        ("9a-91", "9А-91.png"),
        ("9a91", "9А-91.png"),
        ("m4a1", "m4a1.png"),
        ("m4 carbine", "M4_Carabine.png"),
        ("m4", "M4_Carabine.png"),
        ("m16a2", "M16.png"),
        ("m16", "M16.png"),
        ("ak-74un", "АКС74У.png"),
        ("ak-74u", "АКС74У.png"),
        ("ak-74у", "АКС74У.png"),
        ("ак-74у", "АКС74У.png"),
        ("ak (1947)", "Ak1947.png"),
        ("ak-47", "Ak1947.png"),
        ("ak-12", "ak12ratnik.png"),
        ("ak12", "ak12ratnik.png"),
        ("ak-101", "ak-101.png"),
        ("ak101", "ak-101.png"),
        ("ak-102", "ak-102.png"),
        ("ak102", "ak-102.png"),
        ("ak-103", "AK-103.png"),
        ("ak103", "AK-103.png"),
        ("ak-104", "ak104.png"),
        ("ak104", "ak104.png"),
        ("ak-105", "ak105.png"),
        ("ak105", "ak105.png"),
        ("ak-74", "ak74.png"),
        ("ak74", "ak74.png"),
        ("ак-74", "ak74.png"),
        ("ак74", "ak74.png"),
        ("акмс", "AKMS.png"),
        ("akms", "AKMS.png"),
        ("акм", "akm.png"),
        ("akm", "akm.png"),
        ("акс", "Ak1947.png"),
        ("аш 12.7", "asha_17.7.png"),
        ("аш 12,7", "asha_17.7.png"),
        ("ash 12.7", "asha_17.7.png"),
        ("ash12", "asha_17.7.png"),
        ("asha", "asha_17.7.png"),
        ("famas", "FAMAS.png"),
        ("fn 2000", "FN_2000.png"),
        ("fn2000", "FN_2000.png"),
        ("fn scar-h", "FN_SCAR-H.png"),
        ("scar-h", "FN_SCAR-H.png"),
        ("scarh", "FN_SCAR-H.png"),
        ("fn herstal fal", "FN-FAL_belgian.png"),
        ("fn fal", "FN_FAL.png"),
        ("fal", "FN_FAL.png"),
        ("fnc", "fn_fnc.png"),
        ("hk 417", "HK 417.png"),
        ("hk417", "HK 417.png"),
        ("g3/sg1", "G3SG1.png"),
        ("g3sg1", "G3SG1.png"),
        ("g3a3", "G3A3.png"),
        ("g3", "G3A3.png"),
        ("g36k", "Gewehr_G36.png"),
        ("g36", "G36.png"),
        ("hk 416", "hk416.png"),
        ("hk416", "hk416.png"),
        ("l85a2", "L85A2.png"),
        ("l85a1", "l85.png"),
        ("l85", "l85.png"),
        ("sig-552", "SIG-552Commando.png"),
        ("sig552", "SIG-552Commando.png"),
        ("sig schützengewehr 550-1", "SIG_SchützenGewehr 550-1_Sniper.png"),
        ("sig schutzengewehr 550-1", "SIG_SchützenGewehr 550-1_Sniper.png"),
        ("schützengewehr 550", "SIG_SchützenGewehr 550-1_Sniper.png"),
        ("schutzengewehr 550", "SIG_SchützenGewehr 550-1_Sniper.png"),
        ("sg550-1", "SIG_SchützenGewehr 550-1_Sniper.png"),
        ("ssg 550", "SIG_SchützenGewehr 550-1_Sniper.png"),
        ("ssg550", "SIG_SchützenGewehr 550-1_Sniper.png"),
        ("sig 550-1", "SIG_SchützenGewehr 550-1_Sniper.png"),
        ("sig-550", "SIG-550.png"),
        ("sig550", "SIG-550.png"),
        ("sg550", "SIG-550.png"),
        ("оц-14", "ОЦ_14.png"),
        ("гроза", "ОЦ_14.png"),
        ("ots-14", "ОЦ_14.png"),
        ("groza", "ОЦ_14.png"),
        ("ср-3м", "SR-3_Vikhr.png"),
        ("вихрь", "SR-3_Vikhr.png"),
        ("sr-3m", "SR-3_Vikhr.png"),
        ("sr-3", "SR-3_Vikhr.png"),
        ("vikhr", "SR-3_Vikhr.png"),
        ("sr3", "SR-3_Vikhr.png"),
        ("ас «вал»", "as_val.png"),
        ("ас \"вал\"", "as_val.png"),
        ("ас вал", "as_val.png"),
        ("as val", "as_val.png"),
        ("asval", "as_val.png"),
        ("вск-94", "ВСК94.png"),
        ("vsk-94", "ВСК94.png"),
        ("vsk94", "ВСК94.png"),
        ("оц-03", "ОЦ-03_СВУ_Булл-Пап.png"),
        ("сву", "ОЦ-03_СВУ_Булл-Пап.png"),
        ("svu", "ОЦ-03_СВУ_Булл-Пап.png"),
        ("свт-40", "СВТ-40.png"),
        ("svt-40", "СВТ-40.png"),
        ("svt40", "СВТ-40.png"),
        ("mk.14 ebr", "m14ebr.png"),
        ("mk14 ebr", "m14ebr.png"),
        ("mk14", "m14ebr.png"),
        ("m14ebr", "m14ebr.png"),
        ("ebr", "m14ebr.png"),
        ("sr-25", "KAC_SR-25.png"),
        ("sr25", "KAC_SR-25.png"),
        ("kac sr", "KAC_SR-25.png"),
        ("gewehr 43", "Gewehr43.png"),
        ("gewehr43", "Gewehr43.png"),
        ("g43", "Gewehr43.png"),
        ("desert eagle", "IMI_Desert_Eagle.png"),
        ("deagle", "IMI_Desert_Eagle.png"),
        ("galil ar", "Galil.png"),
        ("galil", "Galil.png"),
        ("lr-300", "lr300.png"),
        ("lr300", "lr300.png"),
        ("zm lr", "lr300.png"),
        ("type-63", "Type-63.png"),
        ("type63", "Type-63.png"),
        ("скс", "СКС.png"),
        ("sks", "СКС.png"),
        ("aug a3", "AUGA3.png"),
        ("auga3", "AUGA3.png"),
        ("aug-77", "aug.png"),
        ("aug", "aug.png"),
        ("xm-8", "xm-8.png"),
        ("xm8", "xm-8.png"),
        ("heckler & koch usp", "usp9x19.png"),
        ("usp match 9x19", "usp9x19.png"),
        ("usp match", "usp45acp.png"),
        ("usp", "usp45acp.png"),
        ("beretta 92", "Beretta_92_FS.png"),
        ("m1911", "M1911.png"),
        ("colt m1911", "M1911.png"),
        ("cz 75 auto", "cz75_auto.png"),
        ("cz75 auto", "cz75_auto.png"),
        ("cz 75", "CZ-75.png"),
        ("cz75", "CZ-75.png"),
        ("cz 52", "cz52.png"),
        ("cz52", "cz52.png"),
        ("fnx-45", "FNX_45.png"),
        ("fnx45", "FNX_45.png"),
        ("fnp-45", "FN FNP-45.png"),
        ("fnp45", "FN FNP-45.png"),
        ("glock 17", "Glock_17.png"),
        ("glock17", "Glock_17.png"),
        ("glock 19", "Glock_19.png"),
        ("glock19", "Glock_19.png"),
        ("walther p99", "WaltherP99.png"),
        ("p99", "WaltherP99.png"),
        ("accuracy international l96a1m", "L96A1M.png"),
        ("l96a1m", "L96A1M.png"),
        ("accuracy international l96a1", "AccuracyInternationalL96A1.png"),
        ("l96a1", "AccuracyInternationalL96A1.png"),
        ("barrett m98b", "Barrett_M98B.png"),
        ("m98b", "Barrett_M98B.png"),
        ("barrett m82", "M82.png"),
        ("m82", "M82.png"),
        ("m24 sws", "M24.png"),
        ("m24", "M24.png"),
        ("mosin m1891", "MosinNagant.png"),
        ("mosin", "MosinNagant.png"),
        ("mauser karabiner 98", "Mauser_Karabiner_98_Kurz.png"),
        ("karabiner 98", "Mauser_Karabiner_98_Kurz.png"),
        ("kar98k", "Mauser_Karabiner_98_Kurz.png"),
        ("mauser 98", "Mauser_Karabiner_98_Kurz.png"),
        ("remington 700", "Remington700.png"),
        ("sako trg", "Sako_TRG.png"),
        ("sako", "Sako_TRG.png"),
        ("св-98", "СВ-98.png"),
        ("sv-98", "СВ-98.png"),
        ("sv98", "СВ-98.png"),
        ("walther wa 2000", "WA2000.png"),
        ("wa 2000", "WA2000.png"),
        ("wa2000", "WA2000.png"),
        ("всск", "VSSK_Vikhlop.png"),
        ("выхлоп", "VSSK_Vikhlop.png"),
        ("vssk", "VSSK_Vikhlop.png"),
        ("vykhlop", "VSSK_Vikhlop.png"),
        ("armsel protecta", "Armsel_Protecta.png"),
        ("protecta", "Armsel_Protecta.png"),
                ("байкал mp-133", "MР133.png"),
        ("байкал мр-133", "MР133.png"),
        ("mp-133", "MР133.png"),
        ("mp133", "MР133.png"),
        ("мр-133", "MР133.png"),
        ("мр133", "MР133.png"),
        ("байкал mp-153", "Байкал_MP-153.png"),
        ("mp-153", "Байкал_MP-153.png"),
        ("mp153", "Байкал_MP-153.png"),
        ("fort-500", "fort-500.png"),
        ("fort500", "fort-500.png"),
        ("franchi spas-12", "SPAS12.png"),
        ("spas-12", "SPAS12.png"),
        ("spas12", "SPAS12.png"),
        ("toz-66 sawn", "обрез_тоз66.png"),
        ("toz-66 обрез", "обрез_тоз66.png"),
        ("toz-66", "тоз66.png"),
        ("toz66", "тоз66.png"),
        ("тоз-66 обрез", "обрез_тоз66.png"),
        ("тоз66 обрез", "обрез_тоз66.png"),
        ("тоз-66", "тоз66.png"),
        ("тоз66", "тоз66.png"),
        ("toz-34 sawn", "Обрез_Тоз-34.png"),
        ("toz-34 обрез", "Обрез_Тоз-34.png"),
        ("toz-34", "Тоз34.png"),
        ("toz34", "Тоз34.png"),
        ("тоз-34 обрез", "Обрез_Тоз-34.png"),
        ("тоз34 обрез", "Обрез_Тоз-34.png"),
        ("тоз-34", "Тоз34.png"),
        ("тоз34", "Тоз34.png"),
        ("toz-194", "toz194compact.png"),
        ("toz194", "toz194compact.png"),
        ("mossberg 590", "Mossberg590.png"),
        ("mossberg590", "Mossberg590.png"),
        ("remington 870", "Remington870.png"),
        ("сайга 12s", "Saiga12S.png"),
        ("сайга 12", "Saiga12S.png"),
        ("saiga 12s", "Saiga12S.png"),
        ("saiga12", "Saiga12S.png"),
        ("usas-12", "USAS-12.png"),
        ("usas12", "USAS-12.png"),
        ("вепрь-12", "Вепрь-12.png"),
        ("вепрь 12", "Вепрь-12.png"),
        ("vepr-12", "Вепрь-12.png"),
        ("vepr12", "Вепрь-12.png"),
        ("winchester-1300 trapper", "Winchester-1300Trapper.png"),
        ("winchester 1300 trapper", "Winchester-1300Trapper.png"),
        ("1300 trapper", "Winchester-1300Trapper.png"),
        ("winchester-1300", "Winchester-1300.png"),
        ("winchester 1300", "Winchester-1300.png"),
        ("winchester1300", "Winchester-1300.png"),
        ("f1 fragmentation", "Граната_Ф-1.png"),
        ("граната ф-1", "Граната_Ф-1.png"),
        ("f1", "Граната_Ф-1.png"),
        ("rgd-5", "РГД-5.png"),
        ("rgd5", "РГД-5.png"),
        ("ргд-5", "РГД-5.png"),
        ("ргд5", "РГД-5.png"),
        ("smoke grenade", "Дымовая граната.png"),
        ("дымовая граната", "Дымовая граната.png"),
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
        "6х4 Нож.png",
        "9А-91.png",
        "AEK_971.png",
        "AK-103.png",
        "AKMS.png",
        "AUGA3.png",
        "AccuracyInternationalL96A1.png",
        "Ak1947.png",
        "Armsel_Protecta.png",
        "Barrett_M98B.png",
        "Beretta_92_FS.png",
        "CZ-75.png",
        "FAMAS.png",
        "FN FNP-45.png",
        "FN-FAL_belgian.png",
        "FNX_45.png",
        "FN_2000.png",
        "FN_FAL.png",
        "FN_SCAR-H.png",
        "G36.png",
        "G3A3.png",
        "G3SG1.png",
        "Galil.png",
        "Gewehr43.png",
        "Gewehr_G36.png",
        "Glock_19.png",
        "HK 417.png",
        "HK_MP5.png",
        "IMI_Desert_Eagle.png",
        "KAC_SR-25.png",
        "L85A2.png",
        "L96A1M.png",
        "M16.png",
        "M1911.png",
        "M24.png",
        "M4_Carabine.png",
        "M60.png",
        "M82.png",
        "Mauser_Karabiner_98_Kurz.png",
        "MosinNagant.png",
        "Mossberg590.png",
        "MР-133.png",
        "PKM.png",
        "PKP_Pecheneg.png",
        "PP-2000.png",
        "RPD.png",
        "RPK_74_SAW.png",
        "RPK_SAW.png",
        "Remington700.png",
        "Remington870.png",
        "SIG-550.png",
        "SIG-552Commando.png",
        "SIG_SchützenGewehr 550-1_Sniper.png",
        "SPAS12.png",
        "SR-3_Vikhr.png",
        "SSG_55X.png",
        "SVD.png",
        "Saiga12S.png",
        "Sako_TRG.png",
        "Type-63.png",
        "USAS-12.png",
        "VSSK_Vikhlop.png",
        "WA2000.png",
        "WaltherP99.png",
        "Winchester-1300.png",
        "Winchester-1300Trapper.png",
        "ak-101.png",
        "ak-102.png",
        "ak104.png",
        "ak105.png",
        "ak12ratnik.png",
        "ak74.png",
        "akm.png",
        "as_val.png",
        "asha_17.7.png",
        "aug.png",
        "axe.png",
        "crossbow.png",
        "cz52.png",
        "cz75_auto.png",
        "fn_fnc.png",
        "fort-500.png",
        "hk416.png",
        "knife.png",
        "l85.png",
        "lr300.png",
        "m14ebr.png",
        "m249saw.png",
        "m4a1.png",
        "m5bayonet.png",
        "machete.png",
        "mp5pg6.png",
        "mp7.png",
        "p90.png",
        "pm.png",
        "pp19.png",
        "pp19_Bizon.png",
        "skorpion.png",
        "sledge.png",
        "toz194compact.png",
        "ump45.png",
        "usp45acp.png",
        "usp9x19.png",
        "vss_vintorez.png",
        "xm-8.png",
        "АКС74У.png",
        "АПС.png",
        "Байкал_MP-153.png",
        "ВСК94.png",
        "Вепрь-12.png",
        "НожРазведчика.png",
        "НожСпецназа.png",
        "ОЦ-03_СВУ_Булл-Пап.png",
        "ОЦ-33_Пернач.png",
        "ОЦ_14.png",
        "Обрез_Тоз-34.png",
        "ОхотничийНож.png",
        "ППШ-41.png",
        "ПожарныйТопор.png",
        "СВ-98.png",
        "СВТ-40.png",
        "СКС.png",
        "Тоз34.png",
        "Топорик.png",
        "абакан.png",
        "обрез_тоз66.png",
        "охотничий_топор.png",
        "тоз66.png",
        "Glock_17.png",
        "QJY-88.png",
        "Граната_Ф-1.png",
        "Дымовая граната.png",
        "РГД-5.png",
    
    
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
            if (nf.Length == 0) continue;
            if (nCode.Equals(nf, StringComparison.OrdinalIgnoreCase) || nName.Equals(nf, StringComparison.OrdinalIgnoreCase))
                return WeaponIcon(file);
        }

        // Мягкое совпадение: имя файла (норм.) содержится в коде/названии или наоборот.
        // Минимальная длина снижает риск ложных совпадений (например, "pm" внутри длинных строк).
        foreach (var file in KnownWeaponFiles)
        {
            var nf = NormalizeCode(Path.GetFileNameWithoutExtension(file));
            if (nf.Length < 4) continue;
            if (nCode.Contains(nf, StringComparison.OrdinalIgnoreCase) || nName.Contains(nf, StringComparison.OrdinalIgnoreCase)
                || nf.Contains(nCode, StringComparison.OrdinalIgnoreCase) || nf.Contains(nName, StringComparison.OrdinalIgnoreCase))
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

        int requiredLoyalty = LoyaltyForDamage(damage);
        bool isToz = w.Code is "toz66" or "toz66sawnoff" or "toz34" or "toz34sawnoff";
        if (isToz) requiredLoyalty = 1;

        int price = PriceFor(damage, w.WeaponClass);
        if (isToz) price = w.Code.Contains("sawn", StringComparison.OrdinalIgnoreCase) ? 1100 : 1300;

        return new Item
        {
            Code = "wpn_" + w.Code,
            Name = w.Name,
            Description = string.Join(" ", description),
            Icon = IconFor(w),
            Type = isGrenade ? ItemType.Misc : isMelee ? ItemType.WeaponMelee : ItemType.WeaponRanged,
            Rarity = RarityFor(w.WeaponClass),
            Price = price,
            Damage = damage,
            Caliber = isGrenade ? "" : (w.Caliber ?? ""),
            Weight = isMelee ? 2.0 : 3.5,
            Lootable = false,
            IsGrenade = isGrenade,
            RequiredLoyalty = requiredLoyalty,
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

    private static void Load()
    {
        try
        {
            var assembly = typeof(WeaponCatalog).Assembly;
            var stream = assembly.GetManifestResourceStream(EmbeddedResourceName);
            if (stream == null) return;
            using var reader = new StreamReader(stream);
            using var doc = JsonDocument.Parse(reader.ReadToEnd());
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var w in root.EnumerateArray()) All.Add(ParseOne(w));
            }
            else
            {
                if (root.TryGetProperty("weapons", out var weapons) && weapons.ValueKind == JsonValueKind.Array)
                    foreach (var w in weapons.EnumerateArray()) All.Add(ParseOne(w));
                if (root.TryGetProperty("traderWeapons", out var tw) && tw.ValueKind == JsonValueKind.Array)
                    foreach (var w in tw.EnumerateArray()) TraderWeapons.Add(ParseOne(w));
            }
        }
        catch
        {
            // Если файл не загрузился — просто без дополнительного оружия.
        }
    }
}
