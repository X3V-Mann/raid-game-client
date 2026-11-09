namespace GameApi.Models;

public class Trader
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Image { get; set; } = "";

    /// <summary>Какие категории предметов торговец покупает. Пусто + BuysEverything — покупает всё.</summary>
    public List<ItemType> Buys { get; set; } = new();
    public bool BuysEverything { get; set; } = false;

    /// <summary>Доля цены, которую торговец платит при покупке (0.5 = 50%).</summary>
    public double BuyRate { get; set; } = 0.5;

    /// <summary>Наценка торговца при продаже (1.0 = базовая цена).</summary>
    public double PriceMultiplier { get; set; } = 1.0;

    public List<Item> Stock { get; set; } = new();
    public List<BarterOffer> Barter { get; set; } = new();

    // Заполняется на лету в GET /traders
    public int LoyaltyLevel { get; set; } = 1;
    public int LoyaltyRep { get; set; }
    public int LoyaltyCurrent { get; set; }
    public int LoyaltyNext { get; set; }
    public bool IsUnlocked { get; set; } = true;
    public List<DialogueLine> Dialogue { get; set; } = new(); // заглушка/контент диалога
    public List<DialogueNode> DialogueTree { get; set; } = new(); // интерактивный диалог (STALKER-style)
}

public class DialogueLine
{
    public string Speaker { get; set; } = "";
    public string Text { get; set; } = "";
    public string? GivesQuestId { get; set; }
}

public class DialogueNode
{
    public string Id { get; set; } = "";
    public string Speaker { get; set; } = "";
    public string Text { get; set; } = "";
    public List<DialogueChoice> Choices { get; set; } = new();
}

public class DialogueChoice
{
    public string Text { get; set; } = "";
    public string? NextId { get; set; }
    /// <summary>Действие: accept_quest, complete_quest, talk, close</summary>
    public string? Action { get; set; }
    public string? ActionId { get; set; }
}

public class ItemStack
{
    public string Code { get; set; } = "";
    public int Count { get; set; } = 1;
}

public class BarterOffer
{
    public string Id { get; set; } = "";
    public string GivesCode { get; set; } = "";
    public int GivesCount { get; set; } = 1;
    public int Money { get; set; } = 0;
    public List<ItemStack> Cost { get; set; } = new();
    public int RequiredLoyalty { get; set; } = 0;
}
