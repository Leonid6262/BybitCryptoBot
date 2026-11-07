using BybitCryptoBot.Core;
using System.Transactions;

namespace BybitCryptoBot.Core;

public enum TradeAction { None, Buy, Sell }

public readonly record struct Signal(
    TradeAction Action,
    decimal Price,
    decimal? EntryPrice,
    decimal? MinSellPrice,
    decimal? StopPrice);

public class Strategy
{
    private readonly Position _position = new();
    private readonly decimal _imbalanceThreshold;
    private readonly decimal _minVolumeThreshold;
    private readonly decimal _buyAmountUsdt;
    private readonly decimal _targetProfitUsdt;
    private readonly decimal _buyFee, _sellFee;
    private readonly decimal _stopLossPercent;
    private readonly int _timeoutSeconds;

    public Strategy(decimal imbalanceThreshold, decimal minVolumeThreshold,
                    decimal buyAmountUsdt, decimal targetProfitUsdt,
                    decimal buyFee, decimal sellFee,
                    decimal stopLossPercent, int timeoutSeconds)
    { 
        _imbalanceThreshold = imbalanceThreshold;
        _minVolumeThreshold = minVolumeThreshold;
        _buyAmountUsdt = buyAmountUsdt;
        _targetProfitUsdt = targetProfitUsdt;
        _buyFee = buyFee;
        _sellFee = sellFee;
        _stopLossPercent = stopLossPercent;
        _timeoutSeconds = timeoutSeconds;
}

    public Signal OnTick(TradeWindow.Snapshot s, DateTime now)
    {
        // 1) Выход, если позиция открыта
        if (_position.IsOpen)
        {
            var reason = _position.Evaluate(s.Price, now, _timeoutSeconds);
            if (reason != ExitReason.None)
            {
                var exit = new Signal(
                    TradeAction.Sell,
                    s.Price,
                    _position.EntryPrice,
                    _position.MinSellPrice,
                    _position.StopPrice);
                _position.Close();
                return exit;
            }

            // позиция открыта, но выходить рано — сообщаем «держим» + текущие уровни
            return new Signal(
                TradeAction.None,
                s.Price,
                _position.EntryPrice,
                _position.MinSellPrice,
                _position.StopPrice);
        }

        // 2) Вход
        if (!s.IsReady)
            return new Signal(TradeAction.None, s.Price, null, null, null);

        if (s.TotalVolume < _minVolumeThreshold)
            return new Signal(TradeAction.None, s.Price, null, null, null);

        if (s.PercentSell > _imbalanceThreshold && s.Price < s.Vwap)
        {
            decimal qty = _buyAmountUsdt / s.Price;
            _position.Open(s.Price, qty, _targetProfitUsdt,
                           _buyFee, _sellFee, _stopLossPercent, now);

            return new Signal(
                TradeAction.Buy,
                s.Price,
                _position.EntryPrice,
                _position.MinSellPrice,
                _position.StopPrice);
        }

        return new Signal(TradeAction.None, s.Price, null, null, null);
    }
}

