
namespace BybitCryptoBot.Models;
public class TradeRecord
{
    public DateTime Time { get; set; }
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public bool IsBuy { get; set; }
}
