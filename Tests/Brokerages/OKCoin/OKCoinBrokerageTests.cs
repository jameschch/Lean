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
using RestSharp;

namespace QuantConnect.Tests.Brokerages.OKCoin
{
    [TestFixture/*, Ignore("This test requires a configured and active account")*/]
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
            get { return 800m; }
        }

        /// <summary>
        ///     Gets a low price for the specified symbol so a limit buy won't fill
        /// </summary>
        protected override decimal LowPrice
        {
            get { return 400m; }
        }

        protected virtual string SpotOrFuture
        {
            get
            {
                return "spot";
            }
        }
        #endregion

        protected override IBrokerage CreateBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider)
        {
            string apiSecret = Config.Get("okcoin-api-secret");
            string apiKey = Config.Get("okcoin-api-key");
            string url = Config.Get("okcoin-wss-international", "wss://real.okcoin.com:10440/websocket/okcoinapi");
            string restUrl = Config.Get("okcoin-rest-international", "https://www.okcoin.com/api/v1");
            var webSocketClient = new WebSocketWrapper();
            var orderSocketClient = new WebSocketWrapper();
            var factory = new OKCoinWebsocketsFactory();
            var rest = new RestClient(restUrl);
            var http = new OKCoinHttpClient(restUrl);
            //todo: rest client
            return new OKCoinBrokerage(url, webSocketClient, factory, rest, new OKCoinRestFactory(), http, "USD", apiKey, apiSecret, SpotOrFuture, false, securityProvider);
        }

        protected override decimal GetAskPrice(Symbol symbol)
        {
            return 0;
        }

        protected override decimal GetDefaultQuantity()
        {
            return 0.01m;
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
                };
            }
        }

    }
}
