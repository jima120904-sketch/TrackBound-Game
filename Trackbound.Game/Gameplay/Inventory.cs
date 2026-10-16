namespace Trackbound.Gameplay;

public sealed class Inventory
{
    public const int StackLimit = 99;
    public const int SlotLimit = 32;

    private readonly Dictionary<ItemId, int> _items = new();

    public IReadOnlyDictionary<ItemId, int> Items => _items;
    public int Count(ItemId item) => _items.TryGetValue(item, out var count) ? count : 0;

    public int Add(ItemId item, int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        var current = Count(item);
        if (current == 0 && _items.Count >= SlotLimit)
        {
            return 0;
        }

        var accepted = Math.Min(amount, StackLimit - current);
        if (accepted > 0)
        {
            _items[item] = current + accepted;
        }
        return accepted;
    }

    public bool Has(ItemId item, int amount) => amount >= 0 && Count(item) >= amount;

    public void Clear()
    {
        _items.Clear();
    }

    public bool Remove(ItemId item, int amount)
    {
        if (amount <= 0 || !Has(item, amount))
        {
            return false;
        }

        var remaining = Count(item) - amount;
        if (remaining == 0)
        {
            _items.Remove(item);
        }
        else
        {
            _items[item] = remaining;
        }
        return true;
    }
}