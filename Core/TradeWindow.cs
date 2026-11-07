namespace BybitCryptoBot.Core;

using BybitCryptoBot.Models;

public class TradeWindow
{
    private readonly int _windowSeconds;
    private readonly Queue<TradeRecord> _history = new();
    private readonly object _lock = new();

    private decimal _totalValue;
    private decimal _totalVolume;
    private decimal _buyVolume;
    private decimal _sellVolume;

    public TradeWindow(int windowSeconds) => _windowSeconds = windowSeconds;

    // Снимок для рендера и стратегии — без длинных блокировок
    public record Snapshot(
        DateTime LastTradeTime,
        decimal Price,
        decimal Vwap,
        decimal TotalVolume,
        decimal BuyVolume,
        decimal SellVolume,
        decimal PercentBuy,
        decimal PercentSell,
        int WindowSeconds,
        bool IsReady);

    public void Add(TradeRecord record)
    {
        lock (_lock)
        {
            _history.Enqueue(record);
            _totalValue += record.Price * record.Quantity;
            _totalVolume += record.Quantity;
            if (record.IsBuy) _buyVolume += record.Quantity;
            else _sellVolume += record.Quantity;

            while (_history.Count > 0 &&
                   (record.Time - _history.Peek().Time).TotalSeconds > _windowSeconds)
            {
                var old = _history.Dequeue();
                _totalValue -= old.Price * old.Quantity;
                _totalVolume -= old.Quantity;
                if (old.IsBuy) _buyVolume -= old.Quantity;
                else _sellVolume -= old.Quantity;
            }
        }
    }

    public Snapshot GetSnapshot()
    {
        lock (_lock)
        {
            if (_history.Count == 0)
                return new Snapshot(default, 0, 0, 0, 0, 0, 0, 0, 0, false);

            var oldest = _history.Peek();
            var newest = _history.Last();
            double width = (newest.Time - oldest.Time).TotalSeconds;

            decimal vwap = _totalVolume > 0 ? _totalValue / _totalVolume : 0;
            decimal totalFlow = _buyVolume + _sellVolume;
            decimal pBuy = totalFlow > 0 ? _buyVolume / totalFlow * 100 : 0;
            decimal pSell = totalFlow > 0 ? _sellVolume / totalFlow * 100 : 0;

            return new Snapshot(
                newest.Time, newest.Price, vwap,
                _totalVolume, _buyVolume, _sellVolume,
                pBuy, pSell,
                (int)width,
                width >= _windowSeconds * 0.85);
        }
    }
}
