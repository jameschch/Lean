using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Bitfinex.Rest.Json
{
   public class BitfinexCancelReplacePost : BitfinexNewOrderPost
   {
      [JsonProperty("order_id")]
      public long CancelOrderId { get; set; }

   }
}
