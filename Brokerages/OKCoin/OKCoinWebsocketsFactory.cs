using QuantConnect.Brokerages.Bitfinex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.OKCoin
{
    public class OKCoinWebsocketsFactory : IOKCoinWebsocketsFactory
    {

        public virtual IWebSocket CreateInstance(string url)
        {
            IWebSocket instance = new WebSocketWrapper();
            instance.Initialize(url);
            return instance;
        }

    }
}
