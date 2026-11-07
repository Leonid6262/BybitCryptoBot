using BybitCryptoBot.Core;

namespace BybitCryptoBot.Core;

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

public enum Action { None, Buy, Sell }

    public Action OnTick(TradeWindow.Snapshot s, DateTime now)
    {
        // 1) Сначала — проверка выхода, если позиция открыта
        if (_position.IsOpen)
        {
            var reason = _position.Evaluate(s.Price, now, _timeoutSeconds);
            if (reason != ExitReason.None)
            {
                _position.Close();
                return Action.Sell;
            }
            return Action.None; // одну позицию за раз
        }

        // 2) Вход
        if (!s.IsReady) return Action.None;
        if (s.TotalVolume < _minVolumeThreshold) return Action.None;

        if (s.PercentSell > _imbalanceThreshold && s.Price < s.Vwap)
        {
            decimal qty = _buyAmountUsdt / s.Price;
            _position.Open(s.Price, qty, _targetProfitUsdt,
                           _buyFee, _sellFee, _stopLossPercent);
            return Action.Buy;
        }

        return Action.None;
    }
}