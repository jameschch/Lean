using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.Tests.Brokerages.Bitfinex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class BitfinexSecurityMarginModelTests
    {

        [Test]
        public void GenerateMarginCallOrderLongTest()
        {
            var unit = new BitfinexSecurityMarginModel();
            decimal marketPrice = 748m;

            var security = BitfinexTestsHelpers.GetSecurity(marketPrice);
            security.SetMarketPrice(new Tick(DateTime.UtcNow, "BCTUSD", marketPrice, marketPrice));
            security.QuoteCurrency.ConversionRate = marketPrice;
            security.Holdings = new SecurityHolding(security);
            security.Holdings.SetHoldings(1000m, 1.23m);
            security.Holdings.UpdateMarketPrice(marketPrice);

            var actual = unit.GenerateMarginCallOrder(security, 0, 0);

            Assert.AreEqual(-0.01, actual.Quantity);
        }

        [Test]
        public void GenerateMarginCallOrderShortTest()
        {
            var unit = new BitfinexSecurityMarginModel();
            decimal marketPrice = 1501m;

            var security = BitfinexTestsHelpers.GetSecurity(marketPrice);
            security.SetMarketPrice(new Tick(DateTime.UtcNow, "BCTUSD", marketPrice, marketPrice));
            security.QuoteCurrency.ConversionRate = marketPrice;
            security.Holdings = new SecurityHolding(security);
            security.Holdings.SetHoldings(1000m, -1.23m);
            security.Holdings.UpdateMarketPrice(marketPrice);

            var actual = unit.GenerateMarginCallOrder(security, 0, 0);

            Assert.AreEqual(0.01, actual.Quantity);
        }

        [Test]
        public void GenerateMarginCallOrderLongLargeTest()
        {
            var unit = new BitfinexSecurityMarginModel();
            decimal marketPrice = 700m;

            var security = BitfinexTestsHelpers.GetSecurity(marketPrice);
            security.SetMarketPrice(new Tick(DateTime.UtcNow, "BCTUSD", marketPrice, marketPrice));
            security.QuoteCurrency.ConversionRate = marketPrice;
            security.Holdings = new SecurityHolding(security);
            security.Holdings.SetHoldings(1000m, 1.23m);
            security.Holdings.UpdateMarketPrice(marketPrice);

            var actual = unit.GenerateMarginCallOrder(security, 0, 0);

            Assert.AreEqual(-0.06, actual.Quantity);
        }



    }
}
