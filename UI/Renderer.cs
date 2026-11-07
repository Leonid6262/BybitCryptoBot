using BybitCryptoBot.Core;

namespace BybitCryptoBot.UI;

public class Renderer
{
    private readonly TradeWindow _window;
    private readonly Strategy _strategy;
    private readonly object _consoleLock = new();

    public Renderer(TradeWindow window, Strategy strategy)
    { _window = window; _strategy = strategy; }

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var s = _window.GetSnapshot();
                Render(s);
            }
            catch (Exception ex) { /* лог */ }

            try { await Task.Delay(200, ct); }
            catch (TaskCanceledException) { break; }
        }
    }

    private void Render(TradeWindow.Snapshot s)
    {
        lock (_consoleLock)
        {
            Console.SetCursorPosition(0, 0);
            Console.WriteLine($"{s.LastTradeTime} | окно: {s.WindowSeconds} сек   ");
            Console.WriteLine($"VWAP:  {s.Vwap:F2}     ");
            Console.WriteLine($"Volume: {s.TotalVolume:F3} BTC   ");
            Console.WriteLine($"Buy:  {s.BuyVolume:F6} | Sell: {s.SellVolume:F6}     ");
            Console.WriteLine($"%Buy: {s.PercentBuy:F2} | %Sell: {s.PercentSell:F2}     ");
            Console.WriteLine($"Price: {s.Price:F2} | ready: {s.IsReady}   ");
        }
    }
}
