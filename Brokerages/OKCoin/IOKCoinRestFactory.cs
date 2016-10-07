using QuantConnect.Brokerages.Bitfinex;
using RestSharp;

namespace QuantConnect.Brokerages.OKCoin
{
    public interface IOKCoinRestFactory
    {
        IRestClient CreateInstance(string url);
    }
}