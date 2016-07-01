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
using System.Collections.Concurrent;

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
        /// List of known orders
        /// </summary>
        public ConcurrentDictionary<int, Order> CachedOrderIDs = new ConcurrentDictionary<int, Order>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="apiToken"></param>
        public OneBrokerBrokerage(OneBrokerClient client) : base("1Broker")
        {
            this._client = client;
        }

        public override bool IsConnected
        {
            get { return this._tickerToken != null && !this._tickerToken.IsCancellationRequested; }
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

        //todo: symbol mapper.
        //todo: check cross zero orders
        public override bool PlaceOrder(Order order)
        {
            decimal price = -1;
            Jojatekok.OneBrokerAPI.OrderType type = Jojatekok.OneBrokerAPI.OrderType.Market;
            decimal? stopPrice = null;

            if (order.Type == Orders.OrderType.Limit)
            {
                price = ((LimitOrder)order).LimitPrice;
                type = Jojatekok.OneBrokerAPI.OrderType.Limit;
            }
            else if (order.Type == Orders.OrderType.StopLimit)
            {
                price = ((StopLimitOrder)order).LimitPrice;
                type = Jojatekok.OneBrokerAPI.OrderType.Limit;
                stopPrice = ((StopLimitOrder)order).StopPrice;
            }

            var response = _client.Orders.PostOrder(new Jojatekok.OneBrokerAPI.JsonObjects.Order
            (
                 order.Symbol,
                 order.AbsoluteQuantity,
                 order.Direction == OrderDirection.Buy ? TradeDirection.Long : TradeDirection.Short,
                 /*todo: leverage is separate. Can this be ommitted? Otherwise we have to recalculate here from quantity and cash balance.*/
                 0,
                 type,
                 price,
                 /*stop looks like a trailing stop (offset from price) rather than a stop limit price. Check this*/
                 stopPrice
            ));

            if (response == null || response.Id == 0)
            {
                return false;
            }

            order.BrokerId.Add(response.Id.ToString());
            CachedOrderIDs.AddOrUpdate(order.Id, order);

            return true;
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
            _tickerToken = new CancellationTokenSource();
        }

        public override void Disconnect()
        {
            if (this._tickerToken != null)
            {
                this._tickerToken.Cancel();
            }
        }

        //todo: support multiple subscriptions
        private async Task RequestTicker()
        {

            var response = _client.Markets.GetQuotes(new string[] { "BTCUSD" }).First();
            lock (Ticks)
            {
                Ticks.Add(new Tick
                {
                    AskPrice = response.MarketAsk,
                    BidPrice = response.MarketBid,
                    Time = response.TimeUpdated,
                    Value = ((response.MarketAsk + response.MarketBid) / 2),
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
