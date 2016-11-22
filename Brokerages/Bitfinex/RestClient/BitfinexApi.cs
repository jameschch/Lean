using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using QuantConnect.Brokerages.Bitfinex.Rest.Json;
using QuantConnect.Logging;

namespace QuantConnect.Brokerages.Bitfinex.Rest
{
    public partial class BitfinexApi
    {

        #region Declarations
        private readonly string _apiSecret;
        private readonly string _apiKey;

        private const string ApiBfxKey = "X-BFX-APIKEY";
        private const string ApiBfxPayload = "X-BFX-PAYLOAD";
        private const string ApiBfxSig = "X-BFX-SIGNATURE";

        private const string SymbolDetailsRequestUrl = @"/v1/symbols_details";
        private const string BalanceRequestUrl = @"/v1/balances";
        private const string DepthOfBookRequestUrl = @"v1/book/";
        private const string NewOrderRequestUrl = @"/v1/order/new";
        private const string OrderStatusRequestUrl = @"/v1/order/status";
        private const string OrderCancelRequestUrl = @"/v1/order/cancel";
        private const string CancelAllRequestUrl = @"/all";
        private const string CancelReplaceRequestUrl = @"/replace";
        private const string MultipleRequestUrl = @"/multi";

        private const string ActiveOrdersRequestUrl = @"/v1/orders";
        private const string ActivePositionsRequestUrl = @"/v1/positions";
        private const string HistoryRequestUrl = @"/v1/history";
        private const string MyTradesRequestUrl = @"/v1/mytrades";

        private const string LendbookRequestUrl = @"/v1/lendbook/";
        private const string LendsRequestUrl = @"/v1/lends/";

        private const string DepositRequestUrl = @"/v1/deposit/new";
        private const string AccountInfoRequestUrl = "@/v1/account_infos";
        private const string MarginInfoRequstUrl = @"/v1/margin_infos";

        private const string NewOfferRequestUrl = @"/v1/offer/new";
        private const string CancelOfferRequestUrl = @"/v1/offer/cancel";
        private const string OfferStatusRequestUrl = @"/v1/offer/status";

        private const string ActiveOffersRequestUrl = @"/v1/offers";
        private const string ActiveCreditsRequestUrl = @"/v1/credits";

        private const string ActiveMarginSwapsRequestUrl = @"/v1/taken_swaps";
        private const string CloseSwapRequestUrl = @"/v1/swap/close";
        private const string ClaimPosRequestUrl = @"/v1/position/claim";

        private const string DefaulOrderExchangeType = "bitfinex";
        private const string DefaultLimitType = "exchange limit";
        private const string Buy = "buy";
        private const string Sell = "sell";

        public string BaseBitfinexUrl = @"https://api.bitfinex.com";
        #endregion

        public BitfinexApi(string apiSecret, string apiKey)
        {
            _apiSecret = apiSecret;
            _apiKey = apiKey;
            Log.Trace(string.Format("BitfinexAp.BitfinexApi(): Connecting to Bitfinex Api"));
        }

        public virtual BitfinexPublicTickerGet GetPublicTicker(string symbol, BtcInfo.BitfinexUnauthenicatedCallsEnum callType)
        {
            var call = Enum.GetName(typeof(BtcInfo.BitfinexUnauthenicatedCallsEnum), callType);
            var url = @"/v1/" + call.ToLower() + "/" + symbol.ToLower();
            var response = GetBaseResponse(url);

            var publicticketResponseObj = JsonConvert.DeserializeObject<BitfinexPublicTickerGet>(response.Content);
            Log.Trace(string.Format("BitfinexApi.GetSymbols(): {0}", publicticketResponseObj));

            return publicticketResponseObj;
        }

        public virtual BitfinexNewOrderResponse SendOrder(BitfinexNewOrderPost newOrder)
        {
            IRestResponse response = null;
            try
            {
                newOrder.Request = NewOrderRequestUrl;
                newOrder.Nonce = UnixTimeStampUtc();

                var client = GetRestClient(NewOrderRequestUrl);
                response = GetRestResponse(client, newOrder);

                var newOrderResponseObj = JsonConvert.DeserializeObject<BitfinexNewOrderResponse>(response.Content);

                Log.Trace(string.Format("BitfinexApi.SendOrder(): {0}", newOrder.ToString()));
                Log.Trace(string.Format("BitfinexApi.SendOrder(): Response: {0}", newOrderResponseObj));

                return newOrderResponseObj;
            }
            catch (Exception ex)
            {
                var outer = new Exception(response.Content, ex);
                Log.Error(outer);
                return null;
            }
        }

        public virtual BitfinexOrderStatusResponse CancelOrder(int orderId)
        {
            var cancelPost = new BitfinexOrderStatusPost();
            cancelPost.Request = OrderCancelRequestUrl;

            cancelPost.Nonce = UnixTimeStampUtc();
            cancelPost.OrderId = orderId;

            var client = GetRestClient(cancelPost.Request);
            var response = GetRestResponse(client, cancelPost);
            var orderCancelResponseObj = JsonConvert.DeserializeObject<BitfinexOrderStatusResponse>(response.Content);

            Log.Trace(string.Format("BitfinexApi.CancelOrder():  OrderId: {0}, Response From Exchange: {1}", orderId, orderCancelResponseObj.ToString()));

            return orderCancelResponseObj;
        }

        public virtual BitfinexCancelReplaceOrderResponse CancelReplaceOrder(int cancelOrderId, BitfinexNewOrderPost newOrder)
        {
            var replaceOrder = new BitfinexCancelReplacePost()
            {
                Amount = newOrder.Amount,
                CancelOrderId = cancelOrderId,
                Exchange = newOrder.Exchange,
                Price = newOrder.Price,
                Side = newOrder.Side,
                Symbol = newOrder.Symbol,
                Type = newOrder.Type
            };
            return CancelReplaceOrder(replaceOrder);
        }

        public virtual BitfinexCancelReplaceOrderResponse CancelReplaceOrder(BitfinexCancelReplacePost replaceOrder)
        {
            replaceOrder.Request = OrderCancelRequestUrl + CancelReplaceRequestUrl;
            replaceOrder.Nonce = UnixTimeStampUtc();

            var client = GetRestClient(replaceOrder.Request);
            var response = GetRestResponse(client, replaceOrder);

            var replaceOrderResponseObj = JsonConvert.DeserializeObject<BitfinexCancelReplaceOrderResponse>(response.Content);
            replaceOrderResponseObj.OriginalOrderId = replaceOrder.CancelOrderId;

            Log.Trace(string.Format("BitfinexApi.CancelReplaceOrder(): {0}"));
            Log.Trace(string.Format("BitfinexApi.CancelReplaceOrder(): Response From Exchange: {0}", replaceOrderResponseObj.ToString()));

            return replaceOrderResponseObj;
        }

        public virtual BitfinexOrderStatusResponse[] GetActiveOrders()
        {
            var activeOrdersPost = new BitfinexPostBase();
            activeOrdersPost.Request = ActiveOrdersRequestUrl;
            activeOrdersPost.Nonce = UnixTimeStampUtc();

            var client = GetRestClient(activeOrdersPost.Request);
            var response = GetRestResponse(client, activeOrdersPost);
            if (response.Content != "[]" && response.Content.StartsWith("["))
            {
                var activeOrdersResponseObj = JsonConvert.DeserializeObject<BitfinexOrderStatusResponse[]>(response.Content);

                foreach (var activeOrder in activeOrdersResponseObj)
                    Log.Trace(string.Format("BitfinexApi.GetActiveOrders(): Order: {0}", activeOrder.ToString()));

                return activeOrdersResponseObj;
            }
            return null;
        }

   

        public virtual IList<BitfinexBalanceResponse> GetBalances()
        {
            try
            {
                var balancePost = new BitfinexPostBase();
                balancePost.Request = BalanceRequestUrl;
                balancePost.Nonce = UnixTimeStampUtc();

                var client = GetRestClient(BalanceRequestUrl);
                var response = GetRestResponse(client, balancePost);

                var balancesObj = JsonConvert.DeserializeObject<IList<BitfinexBalanceResponse>>(response.Content);

                foreach (var balance in balancesObj)
                    Log.Trace("BitfinexApi.GetBalances(): " + balance.ToString());

                return balancesObj;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null;
            }
        }


   

        public virtual IList<BitfinexMarginPositionResponse> GetActivePositions()
        {
            var activePositionsPost = new BitfinexPostBase();
            activePositionsPost.Request = ActivePositionsRequestUrl;
            activePositionsPost.Nonce = UnixTimeStampUtc();

            var client = GetRestClient(activePositionsPost.Request);
            var response = GetRestResponse(client, activePositionsPost);

            var activePositionsResponseObj = JsonConvert.DeserializeObject<IList<BitfinexMarginPositionResponse>>(response.Content);

            foreach (var activePos in activePositionsResponseObj)
                Log.Trace(string.Format("BitfinexApi.GetActivePositions(): {0}", activePos));

            return activePositionsResponseObj;
        }

       

        private RestRequest GetRestRequest(object obj)
        {
            var jsonObj = JsonConvert.SerializeObject(obj);
            var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonObj));
            var request = new RestRequest();
            request.Method = Method.POST;
            request.AddHeader(ApiBfxKey, _apiKey);
            request.AddHeader(ApiBfxPayload, payload);
            request.AddHeader(ApiBfxSig, QuantConnect.Brokerages.Bitfinex.BitfinexBrokerage.GetHexHashSignature(payload, _apiSecret));
            return request;
        }

        private IRestResponse GetRestResponse(RestClient client, object obj)
        {
            var response = client.Execute(GetRestRequest(obj));
            CheckToLogError(response);
            return response;
        }

        private void CheckToLogError(IRestResponse response)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    break;
                case HttpStatusCode.BadRequest:
                    var errorMsgObj = JsonConvert.DeserializeObject<ErrorResponse>(response.Content);
                    Log.Trace("BitfinexApi.CheckToLogError(): " + errorMsgObj.Message);
                    break;
                default:
                    Log.Trace("BitfinexApi.CheckToLogError(): " + response.StatusCode + " - " + response.Content);
                    break;
            }
        }

        private RestClient GetRestClient(string requestUrl)
        {
            var client = new RestClient();
            var url = BaseBitfinexUrl + requestUrl;
            client.BaseUrl = new Uri(url);
            return client;
        }

        private IRestResponse GetBaseResponse(string url)
        {
            try
            {
                var client = new RestClient();
                client.BaseUrl = new Uri(BaseBitfinexUrl);
                var request = new RestRequest();
                request.Resource = url;
                IRestResponse response = client.Execute(request);

                CheckToLogError(response);
                return response;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null;
            }
        }

        public static string UnixTimeStampUtc()
        {
            long unixTimeStamp;
            DateTime currentTime = DateTime.Now;
            DateTime dt = currentTime.ToUniversalTime();
            DateTime unixEpoch = new DateTime(1970, 1, 1);
            unixTimeStamp = (long)((dt.Subtract(unixEpoch)).TotalMilliseconds * 1000000D);
            return unixTimeStamp.ToString();
        }

    }
}
