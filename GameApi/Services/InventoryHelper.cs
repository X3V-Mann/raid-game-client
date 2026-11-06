using GameApi.Models;

namespace GameApi.Services;

public static class InventoryHelper
{
    public static bool IsStackable(Item item) =>
        item.Type is ItemType.Food
            or ItemType.Drink
            or ItemType.Medicine
            or ItemType.Material
            or ItemType.Misc
            or ItemType.Ammo;

    public static bool AddToInventory(Player p, Item item)
    {
        if (IsStackable(item))
        {
            var existing = p.Inventory.FirstOrDefault(i => i.Code == item.Code);
            if (existing != null)
            {
                existing.Count += item.Count;
                return true;
            }
        }

        if (p.Inventory.Count >= p.InventoryCapacity) return false;
        if (item.Count < 1) item.Count = 1;
        p.Inventory.Add(item);
        return true;
    }

    public static bool RemoveFromInventory(Player p, string itemId, int count = 1)
    {
        var item = p.Inventory.FirstOrDefault(i => i.Id == itemId);
        if (item == null) return false;

        if (item.Count <= count) p.Inventory.Remove(item);
        else item.Count -= count;
        return true;
    }

    public static int RemoveAllByCode(Player p, string code, int maxCount = int.MaxValue)
    {
        int removed = 0;
        var toRemove = new List<Item>();
        foreach (var item in p.Inventory.Where(i => i.Code == code))
        {
            if (removed >= maxCount) break;
            int take = Math.Min(item.Count, maxCount - removed);
            item.Count -= take;
            removed += take;
            if (item.Count <= 0) toRemove.Add(item);
        }
        foreach (var i in toRemove) p.Inventory.Remove(i);
        return removed;
    }

    public static int CountByCode(Player p, string code)
        => p.Inventory.Where(i => i.Code == code).Sum(i => i.Count);
}