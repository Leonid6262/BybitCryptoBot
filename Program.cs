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
                var signal = strategy.OnTick(snap, t.Timestamp);

                if (signal.Action != TradeAction.None)
                {
                    // Логируем только на сигналах (вход/выход), не на каждом тике
                    string minSell = signal.MinSellPrice is decimal m ? $", MinSell {m:F2}" : "";
                    string stop = signal.StopPrice is decimal sp ? $", Stop {sp:F2}" : "";
                    File.AppendAllText("signals.log",
                                        $"[{t.Timestamp:HH:mm:ss}] {signal.Action} @ {signal.Price:F2} " +
                                        $"MinSell={signal.MinSellPrice:F2} Stop={signal.StopPrice:F2}\n");
                    Console.WriteLine(
                        $"[{t.Timestamp:HH:mm:ss}] {signal.Action} @ {signal.Price:F2}  " +
                        $"(VWAP {snap.Vwap:F2}, %Sell {snap.PercentSell:F1}{minSell}{stop})");
                }
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
