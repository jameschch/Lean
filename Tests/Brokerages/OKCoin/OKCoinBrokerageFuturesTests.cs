using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Brokerages.OKCoin;
using NUnit.Framework;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Configuration;
using QuantConnect.Orders;
using System.Reflection;
using Moq;
using QuantConnect.Brokerages.Bitfinex;
using RestSharp;

namespace QuantConnect.Tests.Brokerages.OKCoin
{
    [TestFixture/*, Ignore("This test requires a configured and active account")*/]
    public class OKCoinBrokerageFuturesTests : OKCoinBrokerageTests
    {

        protected override string SpotOrFuture
        {
            get
            {
                return "future";
            }
        }

    }
}
