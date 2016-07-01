using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Securities;
using QuantConnect.Orders;
using QuantConnect.Data;
using QuantConnect.Packets;
using Jojatekok.OneBrokerAPI;
using QuantConnect.Data.Market;
using System.Threading;

namespace QuantConnect.Brokerages.OneBroker
{
    public class OneBrokerBrokerage : Brokerage, IDataQueueHandler
    {

        OneBrokerClient _client;
        /// <summary>
        /// Ticks collection
        /// </summary>
        protected List<Tick> Ticks = new List<Tick>();
        CancellationTokenSource _tickerToken;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="apiToken"></param>
        public OneBrokerBrokerage(string apiToken) : base("1Broker")
        {
            this._client = new OneBrokerClient(apiToken);
        }

        public override bool IsConnected
        {
            get
            {
                throw new NotImplementedException();
            }
        }


        public override List<Holding> GetAccountHoldings()
        {
            throw new NotImplementedException();
        }

        public override List<Cash> GetCashBalance()
        {
            //todo: get btcusd rate
            decimal conversionRate = 1;
            var response = _client.Account.GetAccountInfo();
            return new List<Cash> { new Cash("BTC", decimal.Parse(response.BalanceInBitcoins), conversionRate) };
        }

        public override List<Order> GetOpenOrders()
        {
            throw new NotImplementedException();
        }

        public override bool PlaceOrder(Order order)
        {
            throw new NotImplementedException();
        }

        public override bool UpdateOrder(Order order)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get queued tick data
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Data.BaseData> GetNextTicks()
        {
            lock (Ticks)
            {
                var copy = Ticks.ToArray();
                Ticks.Clear();
                return copy;
            }
        }

        //todo: multiple subscriptions
        public void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            var task = Task.Run(() => { this.RequestTicker(); }, _tickerToken.Token);
        }

        public void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            //stop polling
        }

        public override bool CancelOrder(Order order)
        {
            throw new NotImplementedException();
        }

        public override void Connect()
        {
            throw new NotImplementedException();
        }

        public override void Disconnect()
        {
            throw new NotImplementedException();
        }

        //todo: support multiple subscriptions
        private async Task RequestTicker()
        {

            var response = _client.Markets.GetQuotes(new string[] { "BTCUSD" }).First();
            lock (Ticks)
            {
                Ticks.Add(new Tick
                {
                    //todo: "monetary" amounts should be decimal
                    AskPrice = (decimal)response.MarketAsk,
                    BidPrice = (decimal)response.MarketBid,
                    Time = response.TimeUpdated,
                    Value = (decimal)((response.MarketAsk + response.MarketBid) / 2),
                    TickType = TickType.Quote,
                    Symbol = Symbol.Create("BTCUSD", SecurityType.Forex, Market.OneBroker),
                    DataType = MarketDataType.Tick
                });
            }
            if (!_tickerToken.IsCancellationRequested)
            {
                //todo: configurable polling interval
                await Task.Delay(8000, _tickerToken.Token);
                await RequestTicker();
            }
        }

    }
}
