using Moq;
using QuantConnect.Brokerages.Bitfinex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.OKCoin
{
    public class OKCoinMockWebsocketsFactory : IOKCoinWebsocketsFactory
    {

        public virtual IWebSocket CreateInstance(string url)
        {
            return new Mock<IWebSocket>().Object;
        }

    }
}
