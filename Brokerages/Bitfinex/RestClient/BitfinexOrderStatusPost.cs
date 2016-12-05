﻿
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Bitfinex.Rest.Json
{
   public class BitfinexOrderStatusPost : BitfinexPostBase
   {
      /// <summary>
      /// This class can be used to send a cancel message in addition to 
      /// retrieving the current status of an order.
      /// </summary>
      [JsonProperty("order_id")]
      public int OrderId { get; set; }
   }
}