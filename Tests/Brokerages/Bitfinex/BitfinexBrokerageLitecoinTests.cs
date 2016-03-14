using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Brokerages.Bitfinex;
using NUnit.Framework;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Configuration;
using TradingApi.ModelObjects.Bitfinex.Json;
using QuantConnect.Orders;
using System.Reflection;
using Moq;


namespace QuantConnect.Tests.Brokerages.Bitfinex
{
    [TestFixture/*, Ignore("This test requires a configured and active account")*/]
    public class BitfinexBrokerageLitecoinTests : BitfinexBrokerageTests
    {

        protected override Symbol Symbol
        {
            get { return Symbol.Create("LTCBTC", SecurityType.Forex, Market.Bitfinex); }
        }

        /// <summary>
        ///     Gets a high price for the specified symbol so a limit sell won't fill
        /// </summary>
        protected override decimal HighPrice
        {
            get { return 200m; }
        }

        /// <summary>
        ///     Gets a low price for the specified symbol so a limit buy won't fill
        /// </summary>
        protected override decimal LowPrice
        {
            get { return 0.001m; }
        }

        protected override int GetDefaultQuantity()
        {
            return 10;
        }


    }
}
