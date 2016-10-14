using System.Collections.Generic;

namespace QuantConnect.Brokerages.OKCoin
{
    public interface IOKCoinHttpClient
    {
        string Post(string url, Dictionary<string, string> args);
    }
}