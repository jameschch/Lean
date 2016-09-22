using QuantConnect.Brokerages.Bitfinex;

namespace QuantConnect.Brokerages.OKCoin
{
    public interface IOKCoinWebsocketsFactory
    {
        IWebSocket CreateInstance(string url);
    }
}