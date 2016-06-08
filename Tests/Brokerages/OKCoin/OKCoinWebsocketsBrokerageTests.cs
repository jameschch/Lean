using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Brokerages.OKCoin;
using NUnit.Framework;
using Moq;
namespace QuantConnect.Tests.Brokerages.OKCoin
{
    [TestFixture()]
    public class OKCoinWebsocketsBrokerageTests
    {
        [Test()]
        public void SubscribeTest()
        {
            var factory = new OKCoinBrokerageFactory();
            OKCoinWebsocketsBrokerage unit = (OKCoinWebsocketsBrokerage)factory.CreateBrokerage(new Packets.LiveNodePacket { BrokerageData = factory.BrokerageData },
               new Mock<Interfaces.IAlgorithm>().Object);

            var symbol = new List<Symbol> { Symbol.Create("BTCUSD", SecurityType.Forex, Market.OKCoin) };

            unit.Subscribe(null, symbol);


        }
    }
}
