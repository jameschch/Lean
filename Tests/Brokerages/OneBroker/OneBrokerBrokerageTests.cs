using Jojatekok.OneBrokerAPI;
using Jojatekok.OneBrokerAPI.JsonObjects;
using Moq;
using NUnit.Framework;
using QuantConnect.Brokerages.OneBroker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.OneBroker.Tests
{
    [TestFixture()]
    public class OneBrokerBrokerageTests
    {
        Mock<OneBrokerClient> mockClient;
        OneBrokerBrokerage unit;

        public OneBrokerBrokerageTests()
        {
            mockClient = new Mock<OneBrokerClient>(It.IsAny<string>());
            unit = new OneBrokerBrokerage(mockClient.Object);
        }

        [Test()]
        public void OneBrokerBrokerageTest()
        {
            Assert.Fail();
        }

        [Test()]
        public void GetAccountHoldingsTest()
        {
            Assert.Fail();
        }

        [Test()]
        public void GetCashBalanceTest()
        {
            decimal expected = 123.456m;
            var info = new Mock<AccountInfo>();
            info.Setup(i => i.BalanceInBitcoins).Returns(expected.ToString());
            mockClient.Setup(c => c.Account.GetAccountInfo()).Returns(info.Object);
            var actual = unit.GetCashBalance();

            Assert.AreEqual(expected, actual.Where(a => a.Symbol == "BTC").Single().Amount);
        }

        [Test()]
        public void GetOpenOrdersTest()
        {
            Assert.Fail();
        }

        [Test()]
        public void PlaceOrderMarketTest()
        {
            var order = new Orders.MarketOrder
            {
                Id = 1,
                Price = 123,
                Quantity = 456,
                Status = Orders.OrderStatus.New,
                Symbol = "BTCUSD"
            };

            int brokerId = 123;
            Order brokerOrder = null;
            mockClient.Setup(c => c.Orders.PostOrder(It.IsAny<Order>())).Callback<Order>((o) =>
            {
                brokerOrder = o;
                o.Id = (ulong)brokerId;
            }).Returns<Order>(o => o);

            unit.PlaceOrder(order);

            Assert.IsTrue(unit.CachedOrderIDs[1].BrokerId.Contains(brokerId.ToString()));
            Assert.AreEqual(order.AbsoluteQuantity, brokerOrder.AmountMargin);

        }

        [Test()]
        public void PlaceOrderLimitTest()
        {
            Assert.Fail();
        }

        [Test()]
        public void PlaceOrderStopTest()
        {
            Assert.Fail();
        }

        [Test()]
        public void UpdateOrderTest()
        {
            Assert.Fail();
        }

        [Test()]
        public void GetNextTicksTest()
        {
            Assert.Fail();
        }

        [Test()]
        public void SubscribeTest()
        {
            Assert.Fail();
        }

        [Test()]
        public void UnsubscribeTest()
        {
            Assert.Fail();
        }

        [Test()]
        public void CancelOrderTest()
        {
            Assert.Fail();
        }

        [Test()]
        public void ConnectTest()
        {
            Assert.Fail();
        }

        [Test()]
        public void DisconnectTest()
        {
            Assert.Fail();
        }
    }
}