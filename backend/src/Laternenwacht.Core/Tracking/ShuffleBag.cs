namespace Laternenwacht.Core.Tracking;

/// <summary>
/// "Wundertüte": liefert Elemente in zufälliger Reihenfolge, jedes genau einmal pro Durchgang.
/// Beim Neumischen wird vermieden, dass das zuletzt gezogene Element sofort wiederkommt.
/// So erscheint dasselbe Meme nicht zweimal hintereinander, und alle kommen gleich oft dran.
/// </summary>
public sealed class ShuffleBag<T>
{
    private readonly List<T> _items;
    private readonly Random _random;
    private readonly List<int> _pending = [];
    private int _lastIndex = -1;

    public ShuffleBag(IEnumerable<T> items, Random? random = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        _items = items.ToList();
        _random = random ?? Random.Shared;
    }

    public int Count => _items.Count;

    /// <summary>Zieht das nächste Element; bei leerer Tüte <c>false</c>.</summary>
    public bool TryNext(out T item)
    {
        if (_items.Count == 0)
        {
            item = default!;
            return false;
        }

        if (_pending.Count == 0)
        {
            Refill();
        }

        var index = _pending[^1];
        _pending.RemoveAt(_pending.Count - 1);
        _lastIndex = index;
        item = _items[index];
        return true;
    }

    private void Refill()
    {
        _pending.AddRange(Enumerable.Range(0, _items.Count));

        // Fisher-Yates
        for (var i = _pending.Count - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (_pending[i], _pending[j]) = (_pending[j], _pending[i]);
        }

        // Gezogen wird vom Ende: das zuletzt gezeigte Element darf nicht als erstes kommen.
        if (_pending.Count > 1 && _pending[^1] == _lastIndex)
        {
            (_pending[^1], _pending[0]) = (_pending[0], _pending[^1]);
        }
    }
}
