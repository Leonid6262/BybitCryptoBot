using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;
using BybitCryptoBot.Core;
using BybitCryptoBot.Models;
using BybitCryptoBot.UI;

namespace BybitCryptoBot;

class Program
{
    static async Task Main(string[] args)
    {
        var window = new TradeWindow(60);   // [sec]
        var strategy = new Strategy(
                imbalanceThreshold: 60m,
                minVolumeThreshold: 0.5m,
                buyAmountUsdt: 400m,
                targetProfitUsdt: 0.2m,
                buyFee: 0.001m,
                sellFee: 0.001m,
                stopLossPercent: 0.5m,      //
                timeoutSeconds: 300);       // 

        var renderer = new Renderer(window, strategy);
        using var cts = new CancellationTokenSource();
        var renderTask = renderer.RunAsync(cts.Token);  // рендер раз в 200 мс

        using var socketClient = new BybitSocketClient();
        var sub = await socketClient.V5SpotApi.SubscribeToTradeUpdatesAsync(
        "BTCUSDT",
        data =>
        {
            foreach (var t in data.Data)
            {
                var rec = new TradeRecord
                {
                    Time = t.Timestamp,
                    Price = t.Price,
                    Quantity = t.Quantity,
                    IsBuy = t.Side == OrderSide.Buy
                };

                window.Add(rec);                       // быстро, под lock

                var snap = window.GetSnapshot();       // короткий lock
                var action = strategy.OnTick(snap, t.Timestamp);

                if (action != Strategy.Action.None)
                    Console.WriteLine($"[{t.Timestamp:HH:mm:ss}] {action} @ {t.Price:F2}  " +
                                      $"(VWAP {snap.Vwap:F2}, %Sell {snap.PercentSell:F1})");
            }
        });

        if (!sub.Success)
        {
            Console.WriteLine($"Ошибка подписки: {sub.Error}");
            return;
        }

        Console.WriteLine("Подписка оформлена. Enter — выход.");
        Console.ReadLine();          // ← ТЕПЕРЬ здесь, в Main, а не в колбэке
        cts.Cancel();
        await renderTask;
    }
}
