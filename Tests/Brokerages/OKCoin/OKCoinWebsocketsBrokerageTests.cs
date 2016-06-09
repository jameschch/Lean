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
namespace QuantConnect.Tests.Brokerages.OKCoin
{
    [TestFixture()]
    public class OKCoinWebsocketsBrokerageTests
    {

        OKCoinBrokerageFactory factory;
        OKCoinWebsocketsBrokerage live;
        OKCoinWebsocketsBrokerage unit;
        Mock<IWebSocket> orderWebSocket;
        Mock<IWebSocket> webSocket;

        public OKCoinWebsocketsBrokerageTests()
        {
            factory = new OKCoinBrokerageFactory();
            live = (OKCoinWebsocketsBrokerage)factory.CreateBrokerage(new Packets.LiveNodePacket { BrokerageData = factory.BrokerageData },
               new Mock<Interfaces.IAlgorithm>().Object);

            orderWebSocket = new Mock<IWebSocket>();
            webSocket = new Mock<IWebSocket>();
            webSocket.Setup(w => w.Url).Returns(new Uri("wss://real.okcoin.com:10440/websocket/okcoinapi"));

            unit = new OKCoinWebsocketsBrokerage("", webSocket.Object, orderWebSocket.Object, "usd", "", "",
                 "spot", 1m, new Mock<Securities.ISecurityProvider>().Object);
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

            orderWebSocket.Setup(o => o.Send(It.IsAny<string>())).Callback(() => { orderWebSocket.Raise(o => o.OnMessage += null, BitfinexTestsHelpers.GetArgs(json)); });
            var symbol = Symbol.Create("BTCUSD", SecurityType.Forex, Market.OKCoin);
            unit.PlaceOrder(new Orders.MarketOrder { Id = 123, Quantity = 123, Symbol = symbol });

            string actual = unit.CachedOrderIDs.First().Value.BrokerId.First();
            Assert.IsFalse(string.IsNullOrEmpty(actual));

        }


        [Test()]
        public void GetCashBalanceUsdTest()
        {
            string json = System.IO.File.ReadAllText("TestData\\ok_spotusd_userinfo.txt");
            orderWebSocket = new Mock<IWebSocket>();
            orderWebSocket.Setup(o => o.Send(It.IsAny<string>())).Callback(() => { orderWebSocket.Raise(o => o.OnMessage += null, BitfinexTestsHelpers.GetArgs(json)); });
            var actual = unit.GetCashBalance();

        }

        [Test()]
        public void GetCashBalanceCnyTest()
        {
            string json = System.IO.File.ReadAllText("TestData\\ok_spotusd_userinfo.txt");

            orderWebSocket = new Mock<IWebSocket>();
            orderWebSocket.Setup(o => o.Send(It.IsAny<string>())).Callback(() => { orderWebSocket.Raise(o => o.OnMessage += null, BitfinexTestsHelpers.GetArgs(json)); });

            var cnyUnit = new OKCoinWebsocketsBrokerage("", webSocket.Object, orderWebSocket.Object, "cny", "", "",
            "spot", 1m, new Mock<Securities.ISecurityProvider>().Object);

            var actual = cnyUnit.GetCashBalance();


        }

    }
}
