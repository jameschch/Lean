using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Brokerages.OKCoin;
using NUnit.Framework;
using Moq;
using QuantConnect.Brokerages.Bitfinex;
using QuantConnect.Tests.Brokerages.Bitfinex;
using System.Threading;
using QuantConnect.Data.Market;
namespace QuantConnect.Tests.Brokerages.OKCoin
{
    [TestFixture()]
    public class OKCoinWebsocketsBrokerageTests
    {

        #region Declarations
        OKCoinBrokerageFactory factory;
        OKCoinWebsocketsBrokerage live;
        OKCoinWebsocketsBrokerage unit;
        Mock<IWebSocket> webSocket;
        Mock<OKCoinWebsocketsBrokerage> mock;
        Mock<OKCoinMockWebsocketsFactory> mockFactory;
        Mock<IWebSocket> mockWebsockets;
        #endregion

        public OKCoinWebsocketsBrokerageTests()
        {
            factory = new OKCoinBrokerageFactory();

            var data = new Dictionary<string, string>
                {
                    {"apiSecret" ,"123" },
                    {"apiKey" ,"456"},
                    {"url" , "wss://real.okcoin.cn:10440/websocket/okcoinapi"},
                    {"url-international" , "wss://real.okcoin.com:10440/websocket/okcoinapi"},
                    {"spotOrFuture", "spot"},
                    {"baseCurrency", "usd"},
                    {"isTradeTickerEnabled", "true"}
                };


            live = (OKCoinWebsocketsBrokerage)factory.CreateBrokerage(new Packets.LiveNodePacket { BrokerageData = data },
               new Mock<Interfaces.IAlgorithm>().Object);
            mockFactory = new Mock<OKCoinMockWebsocketsFactory>();
            mockWebsockets = new Mock<IWebSocket>();
            mockFactory.Setup(m => m.CreateInstance(It.IsAny<string>())).Returns(mockWebsockets.Object);
        }

        [TestFixtureSetUp()]
        public void Setup()
        {
            webSocket = new Mock<IWebSocket>();

            webSocket.Setup(w => w.Url).Returns(new Uri("wss://real.okcoin.com:10440/websocket/okcoinapi"));

            unit = new OKCoinWebsocketsBrokerage("", webSocket.Object, mockFactory.Object, "usd", "", "abc123", "spot", true, new Mock<Securities.ISecurityProvider>().Object);

            mock = new Mock<OKCoinWebsocketsBrokerage>(It.IsAny<string>(), It.IsAny<IWebSocket>(), It.IsAny<IWebSocket>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IServiceProvider>());
        }

        [Test()]
        public void SubscribeTest()
        {

            var symbol = new List<Symbol> { Symbol.Create("BTCUSD", SecurityType.Forex, Market.OKCoin) };
            live.Subscribe(null, symbol);

        }

        [Test()]
        public void PlaceOrderTest()
        {
            string json = "[{\"channel\":\"ok_spotusd_trade\", \"data\":{ \"order_id\":\"125433029\",\"result\":\"true\"}}]";

            mockWebsockets.Reset();

            mockWebsockets.Setup(o => o.Send(It.IsAny<string>())).Callback(() => { mockWebsockets.Raise(o => o.OnMessage += null, BitfinexTestsHelpers.GetArgs(json)); });
            var symbol = Symbol.Create("BTCUSD", SecurityType.Forex, Market.OKCoin);
            unit.PlaceOrder(new Orders.MarketOrder { Id = 123, Quantity = 123, Symbol = symbol });

            string actual = unit.CachedOrderIDs.First().Value.BrokerId.First();
            Assert.IsFalse(string.IsNullOrEmpty(actual));
        }

        [Test()]
        public void PlaceLimitOrderTest()
        {
            string json = "[{\"channel\":\"ok_spotusd_trade\", \"data\":{ \"order_id\":\"125433029\",\"result\":\"true\"}}]";
            mockWebsockets.Reset();

            mockWebsockets.Setup(o => o.Send(It.IsAny<string>())).Callback(() => { mockWebsockets.Raise(o => o.OnMessage += null, BitfinexTestsHelpers.GetArgs(json)); });
            var symbol = Symbol.Create("BTCUSD", SecurityType.Forex, Market.OKCoin);
            unit.PlaceOrder(new Orders.LimitOrder { Id = 123, Quantity = 123, Symbol = symbol, LimitPrice = 123 });

            string actual = unit.CachedOrderIDs.First().Value.BrokerId.First();
            Assert.IsFalse(string.IsNullOrEmpty(actual));
        }


        [Test()]
        public void GetCashBalanceUsdTest()
        {
            string json = System.IO.File.ReadAllText("TestData\\ok_spotusd_userinfo.txt");
            mockWebsockets.Reset();
            mockWebsockets.Setup(o => o.Send(It.IsAny<string>())).Callback(() => { mockWebsockets.Raise(o => o.OnMessage += null, BitfinexTestsHelpers.GetArgs(json)); });

            var actual = unit.GetCashBalance();

            Assert.AreEqual(actual.Single(a => a.Symbol == "USD").Amount, 456);
            Assert.IsTrue(actual.Single(a => a.Symbol == "BTC").Amount > 0);
            Assert.IsTrue(actual.Single(a => a.Symbol == "LTC").Amount > 0);
            Assert.IsTrue(actual.Single(a => a.Symbol == "CNY").Amount > 0);
        }

        [Test()]
        public void GetCashBalanceCnyTest()
        {
            string json = System.IO.File.ReadAllText("TestData\\ok_spotusd_userinfo.txt");
            mockWebsockets.Reset();

            mockWebsockets.Setup(o => o.Send(It.IsAny<string>())).Callback(() => { mockWebsockets.Raise(o => o.OnMessage += null, BitfinexTestsHelpers.GetArgs(json)); });

            var cnyUnit = new OKCoinWebsocketsBrokerage("", webSocket.Object, mockFactory.Object, "cny", "", "", "spot", false, new Mock<Securities.ISecurityProvider>().Object);

            var actual = cnyUnit.GetCashBalance();

            Assert.IsTrue(actual.Single(a => a.Symbol == "USD").Amount > 0);
            Assert.IsTrue(actual.Single(a => a.Symbol == "BTC").Amount > 0);
            Assert.IsTrue(actual.Single(a => a.Symbol == "LTC").Amount > 0);
            Assert.AreEqual(actual.Single(a => a.Symbol == "CNY").Amount, 246);
        }

        [Test()]
        public void GetAccountHoldingsTest()
        {
            string json = System.IO.File.ReadAllText("TestData\\ok_spotusd_orderinfo.txt");
            mockWebsockets.Reset();

            mockWebsockets.Setup(o => o.Send(It.IsAny<string>())).Callback(() => { mockWebsockets.Raise(o => o.OnMessage += null, BitfinexTestsHelpers.GetArgs(json)); });

            var actual = unit.GetAccountHoldings();

            Assert.AreEqual(actual.Where(a => a.Symbol.Value == "BTCUSD").Sum(a => a.Quantity), 12.35);
            Assert.AreEqual(actual.Single(a => a.Symbol.Value == "BTCCNY").Quantity, 12.34);
            Assert.AreEqual(actual.Single(a => a.Symbol.Value == "LTCUSD").Quantity, -12.34);
        }

        [Test()]
        public void GetOpenOrdersTest()
        {
            string json = System.IO.File.ReadAllText("TestData\\ok_spotusd_orderinfo.txt");
            mockWebsockets.Reset();

            mockWebsockets.Setup(o => o.Send(It.IsAny<string>())).Callback(() => { mockWebsockets.Raise(o => o.OnMessage += null, BitfinexTestsHelpers.GetArgs(json)); });

            var actual = unit.GetOpenOrders();

            Assert.AreEqual(actual.Single(a => a.Symbol.Value == "BTCUSD" && a.Status == Orders.OrderStatus.PartiallyFilled).Quantity, 99);
            Assert.AreEqual(actual.Single(a => a.Symbol.Value == "LTCUSD").Quantity, -0.1);
            Assert.AreEqual(actual.Single(a => a.Symbol.Value == "BTCUSD" && a.Status == Orders.OrderStatus.Submitted).Quantity, 56.78);

        }

        [Test()]
        public void OnMessageTickerTest()
        {
            string json = System.IO.File.ReadAllText("TestData\\ok_sub_spotusd_btc_ticker.txt");

            unit.OnMessage(null, BitfinexTestsHelpers.GetArgs(json));

            var actual = unit.GetNextTicks();

            Assert.AreEqual(2478.4, actual.First().Price);

        }

        [Test()]
        public void OnMessageTradesTest()
        {
            string json = System.IO.File.ReadAllText("TestData\\ok_sub_spotusd_trades.txt");

            var order = new Orders.MarketOrder
            {
                BrokerId = new List<string> { "268013884" },
                Id = 123,
                Quantity = 1.105m
            };

            unit.CachedOrderIDs.AddOrUpdate(order.Id, order);
            unit.FillSplit = new System.Collections.Concurrent.ConcurrentDictionary<int, OKCoinFill>();
            unit.FillSplit.TryAdd(order.Id, new OKCoinFill(order, 1));

            ManualResetEvent raised = new ManualResetEvent(false);

            unit.OrderStatusChanged += (s, e) =>
            {
                Assert.AreEqual("BTCUSD", e.Symbol.Value);
                Assert.AreEqual(0, e.OrderFee);
                Assert.AreEqual(Orders.OrderStatus.PartiallyFilled, e.Status);
                Assert.AreEqual(1, e.FillQuantity);
                raised.Set();
            };

            unit.OnMessage(null, BitfinexTestsHelpers.GetArgs(json));
            Assert.IsTrue(raised.WaitOne(1000));
        }

        [Test()]
        public void OnMessageTradesTickerTest()
        {
            string json = System.IO.File.ReadAllText("TestData\\ok_sub_spotusd_btc_trades.txt");

            unit.OnMessage(null, BitfinexTestsHelpers.GetArgs(json));

            var actual = unit.GetNextTicks();

            Assert.AreEqual(2463.86, actual.First().Price);
            Assert.AreEqual(0, ((Tick)actual.First()).Quantity);
        }

        [Test()]
        public void BuildSign()
        {
            var actual = unit.BuildSign(new Dictionary<string, string> { { "api_key", "abc123" }, { "symbol", "BCTUSD" } });
            Assert.AreEqual("eead4f2c8bb341a14fa3b26e8baca560", actual);
        }


    }
}
