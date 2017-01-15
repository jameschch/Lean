using NUnit.Framework;
using QuantConnect.Algorithm.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.Tests.Brokerages.Bitfinex;
using Moq;
using Jtc.Algorithm;

namespace QuantConnect.Tests.Algorithm.CSharp
{

    [TestFixture]
    public class BaseBitcoinTests
    {

        [Test]
        public void OnDataTest()
        {
            var unit = new Mock<Unit>();
            var holding = BitfinexTestsHelpers.GetSecurity();

            var symbol = Symbol.Create("BTCUSD", SecurityType.Forex, Market.Bitfinex);
            unit.Object.Portfolio.Securities.Add(symbol, holding);
            unit.Object.SetCash(1000);
            unit.Object.Portfolio[symbol].SetHoldings(49, 1);
            unit.Object.Portfolio[symbol].UpdateMarketPrice(49);
            var t = new Ticks();

            unit.Setup(u => u.IsWarmingUp).Returns(false);

            unit.Setup(u => u.Liquidate(It.IsAny<Symbol>())).Verifiable();

            unit.Object.OnData(t);

            unit.Verify();
        }

        public class Unit : BaseBitcoin
        {

            protected virtual Decimal MinimumPosition { get { return 0.5m; } }

            public override void OnData(Tick data)
            {

            }
        }


    }
}
