using Jojatekok.OneBrokerAPI;
using Jojatekok.OneBrokerAPI.JsonObjects;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Brokerages.OneBroker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jojatekok.OneBrokerAPI;

namespace QuantConnect.Brokerages.OneBroker.Tests
{
    [TestFixture()]
    public class OneBrokerBrokerageTests
    {
        Mock<OneBrokerClient> mockClient;
        OneBrokerBrokerage unit;

        private static readonly JsonSerializer JsonSerializer = new JsonSerializer
        {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };

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
            var data = GetJsonData<JsonResponse<IList<Position>>>("TestData\\test_1Broker_positions_list_open.txt");

            mockClient.Setup(c => c.Positions.GetOpenPositions()).Returns(data.Result);

            var actual = unit.GetAccountHoldings().First();

            Assert.AreEqual("GOLD", actual.Symbol.Value);
            Assert.AreEqual(1000, actual.AveragePrice);
            Assert.AreEqual(10, actual.Quantity);
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
            int id = 1;
            var order = new Orders.MarketOrder
            {
                Id = id,
                Price = 123,
                Quantity = 456,
                Status = Orders.OrderStatus.New,
                Symbol = Symbol.Create("BTCUSD", SecurityType.Forex, Market.OneBroker)
            };

            int brokerId = 789;
            Order brokerOrder = null;
            mockClient.Setup(c => c.Orders.PostOrder(It.IsAny<Order>())).Callback<Order>((o) =>
            {
                brokerOrder = o;
                o.Id = (ulong)brokerId;
            }).Returns<Order>(o => o);

            unit.PlaceOrder(order);

            Assert.IsTrue(unit.CachedOrderIDs[id].BrokerId.Contains(brokerId.ToString()));
            Assert.AreEqual(order.AbsoluteQuantity, brokerOrder.AmountMargin);
        }

        [Test()]
        public void PlaceOrderLimitTest()
        {
            int id = 2;
            var order = new Orders.LimitOrder
            {
                Id = id,
                Price = 123,
                LimitPrice = 321,
                Quantity = 456,
                Status = Orders.OrderStatus.New,
                Symbol = Symbol.Create("BTCUSD", SecurityType.Forex, Market.OneBroker)
            };

            int brokerId = 789;
            Order brokerOrder = null;
            mockClient.Setup(c => c.Orders.PostOrder(It.IsAny<Order>())).Callback<Order>((o) =>
            {
                brokerOrder = o;
                o.Id = (ulong)brokerId;
            }).Returns<Order>(o => o);

            unit.PlaceOrder(order);

            Assert.IsTrue(unit.CachedOrderIDs[id].BrokerId.Contains(brokerId.ToString()));
            Assert.AreEqual(order.AbsoluteQuantity, brokerOrder.AmountMargin);
            Assert.AreEqual(order.LimitPrice, brokerOrder.TypeParameter);
        }

        [Test()]
        public void PlaceOrderStopTest()
        {
            int id = 3;
            var order = new Orders.StopLimitOrder
            {
                Id = id,
                Price = 123,
                LimitPrice = 321,
                StopPrice = 654,
                Quantity = 456,
                Status = Orders.OrderStatus.New,
                Symbol = Symbol.Create("BTCUSD", SecurityType.Forex, Market.OneBroker)
            };

            int brokerId = 789;
            Order brokerOrder = null;
            mockClient.Setup(c => c.Orders.PostOrder(It.IsAny<Order>())).Callback<Order>((o) =>
            {
                brokerOrder = o;
                o.Id = (ulong)brokerId;
            }).Returns<Order>(o => o);

            unit.PlaceOrder(order);

            Assert.IsTrue(unit.CachedOrderIDs[id].BrokerId.Contains(brokerId.ToString()));
            Assert.AreEqual(order.AbsoluteQuantity, brokerOrder.AmountMargin);
            Assert.AreEqual(order.LimitPrice, brokerOrder.TypeParameter);
            Assert.AreEqual(order.StopPrice, brokerOrder.StopLoss);
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

        private T GetJsonData<T>(string path)
        {
            using (var stringReader = new StringReader(File.ReadAllText(path)))
            {
                using (var jsonTextReader = new JsonTextReader(stringReader))
                {
                    return (T)JsonSerializer.Deserialize(jsonTextReader, typeof(T));
                }
            }

        }

    }
}