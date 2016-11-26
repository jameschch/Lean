using QuantConnect.Brokerages.Bitfinex;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.OKCoin
{
    public class OKCoinRestFactory : IOKCoinRestFactory
    {

        public virtual IRestClient CreateInstance(string url)
        {
            IRestClient instance = new RestClient(url);
            return instance;
        }

    }
}
