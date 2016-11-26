using QuantConnect.Brokerages.Bitfinex;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;

namespace QuantConnect.Tests.Brokerages.Bitfinex
{
    public class BitfinexTestsHelpers
    {

        public static Security GetSecurity()
        {
            return new Security(SecurityExchangeHours.AlwaysOpen(TimeZones.Utc), CreateConfig(), new Cash(CashBook.AccountCurrency, 1000, 1m), SymbolProperties.GetDefault(CashBook.AccountCurrency));
        }

        private static SubscriptionDataConfig CreateConfig()
        {
            return new SubscriptionDataConfig(typeof(TradeBar), Symbol.Create("BTCUSD", SecurityType.Forex, Market.Bitfinex), Resolution.Minute, TimeZones.Utc, TimeZones.Utc, false, true, false);
        }

        public static void AddOrder(BitfinexBrokerage unit, int id, string brokerId, decimal scaleFactor, int quantity)
        {
            var order = new Orders.MarketOrder { BrokerId = new List<string> { brokerId }, Quantity = quantity, Id = id };
            unit.CachedOrderIDs.TryAdd(1, order);
            unit.FillSplit.TryAdd(id, new BitfinexFill(order, scaleFactor));
        }

        [DebuggerStepThrough]
        public static MessageEventArgs GetArgs(string json)
        {
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            System.Globalization.CultureInfo culture = null;
            MessageEventArgs args = (MessageEventArgs)Activator.CreateInstance(typeof(MessageEventArgs), flags, null, new object[]
            {
                Opcode.Text,
                System.Text.Encoding.UTF8.GetBytes(json)
            }, culture);

            return args;
        }

    }
}
