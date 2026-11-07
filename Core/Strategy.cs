using BybitCryptoBot.Models;

namespace BybitCryptoBot.Core;

public class Strategy
{
    public Strategy(decimal imbalanceThreshold, decimal minVolumeThreshold,
                    decimal buyAmountUsdt, decimal targetProfitUsdt,
                    decimal buyFee, decimal sellFee,
                    decimal stopLossPercent, int timeoutSeconds)
    { /* сохранить поля */ }
    public enum Action { None, Buy, Sell }
    //public Action OnTick(TradeWindow.Snapshot s, DateTime now) { ... }
}
