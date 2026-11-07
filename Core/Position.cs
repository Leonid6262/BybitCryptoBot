namespace BybitCryptoBot.Core;

public enum ExitReason { None, TakeProfit, StopLoss, Timeout }

public class Position
{
    public bool IsOpen { get; private set; }
    public decimal EntryPrice { get; private set; }
    public decimal Quantity { get; private set; }   // в BTC
    public decimal MinSellPrice { get; private set; }
    public decimal StopPrice { get; private set; }
    public DateTime OpenedAt { get; private set; }

    public void Open(decimal entryPrice, decimal quantity, decimal targetProfitUsdt,
                     decimal buyFee, decimal sellFee, decimal stopLossPercent)
    {
        IsOpen = true;
        EntryPrice = entryPrice;
        Quantity = quantity;
        OpenedAt = DateTime.UtcNow;

        // Цена, при которой получим ровно targetProfitUsdt после двух комиссий
        MinSellPrice = (entryPrice * quantity + targetProfitUsdt)
                       / (quantity * (1 - buyFee) * (1 - sellFee));

        // Стоп: -stopLossPercent от входа (по цене последней сделки)
        StopPrice = entryPrice * (1 - stopLossPercent / 100m);
    }

    public ExitReason Evaluate(decimal currentPrice, DateTime now, int timeoutSeconds)
    {
        if (!IsOpen) return ExitReason.None;
        if (currentPrice >= MinSellPrice) return ExitReason.TakeProfit;
        if (currentPrice <= StopPrice) return ExitReason.StopLoss;
        if ((now - OpenedAt).TotalSeconds >= timeoutSeconds) return ExitReason.Timeout;
        return ExitReason.None;
    }

    public void Close() => IsOpen = false;
}