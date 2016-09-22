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

namespace QuantConnect.Tests.Brokerages.OKCoin
{
    [TestFixture, Ignore("This test requires a configured and active account")]
    public class OKCoinBrokerageTests : BrokerageTests
    {

        #region Properties
        protected override Symbol Symbol
        {
            get { return Symbol.Create("BTCUSD", SecurityType.Forex, Market.OKCoin); }
        }

        /// <summary>
        ///     Gets the security type associated with the <see cref="BrokerageTests.Symbol" />
        /// </summary>
        protected override SecurityType SecurityType
        {
            get { return SecurityType.Forex; }
        }

        /// <summary>
        ///     Gets a high price for the specified symbol so a limit sell won't fill
        /// </summary>
        protected override decimal HighPrice
        {
            get { return 9000m; }
        }

        /// <summary>
        ///     Gets a low price for the specified symbol so a limit buy won't fill
        /// </summary>
        protected override decimal LowPrice
        {
            get { return 1000m; }
        }
        #endregion

        protected override IBrokerage CreateBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider)
        {
            string apiSecret = Config.Get("okcoin-api-secret");
            string apiKey = Config.Get("okcoin-api-key");
            string wallet = Config.Get("okcoin-wallet");
            decimal scaleFactor = decimal.Parse(Config.Get("okcoin-scale-factor", "1"));
            string url = Config.Get("okcoin-wss", "wss://real.okcoin.com:10440/websocket/okcoinapi");
            var webSocketClient = new WebSocketWrapper();
            var orderSocketClient = new WebSocketWrapper();
            var factory = new OKCoinWebsocketsFactory();
            return new OKCoinWebsocketsBrokerage(url, webSocketClient, factory, "USD", apiKey, apiSecret, wallet, securityProvider);
        }

        protected override decimal GetAskPrice(Symbol symbol)
        {
            return 0;
        }

        //no stop limit support
        public override TestCaseData[] OrderParameters
        {
            get
            {
                return new[]
                {                    
                    new TestCaseData(new MarketOrderTestParameters(Symbol)).SetName("MarketOrder"),
                    new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice)).SetName("LimitOrder"),
                    new TestCaseData(new StopMarketOrderTestParameters(Symbol, HighPrice, LowPrice)).SetName("StopMarketOrder"),
                };
            }
        }

    }
}
