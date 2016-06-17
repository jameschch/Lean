using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Brokerages;
using NUnit.Framework;

namespace QuantConnect.Tests.Brokerages.Bitfinex
{
    [TestFixture()]
    public class BitfinexBrokerageModelTests
    {

        BitfinexBrokerageModel unit = new BitfinexBrokerageModel(AccountType.Margin);

        [Test()]
        public void CanSubmitOrderTest()
        {
            var security = BitfinexTestsHelpers.GetSecurity();
            BrokerageMessageEvent message;

            var order = new Orders.MarketOrder { Quantity = (int)1m, Symbol = security.Symbol };
            var actual = unit.CanSubmitOrder(security, order, out message);
            Assert.IsTrue(actual);

            order.Quantity = 1m;
            actual = unit.CanSubmitOrder(security, order, out message);
            Assert.IsTrue(actual);

            order.Quantity = 0.01m;
            actual = unit.CanSubmitOrder(security, order, out message);
            Assert.IsTrue(actual);

            order.Quantity = 0.001m;
            actual = unit.CanSubmitOrder(security, order, out message);
            Assert.IsFalse(actual);
        }
    }
}
