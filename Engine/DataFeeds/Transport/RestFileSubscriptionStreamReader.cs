/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/

using System;
using QuantConnect.Logging;
using RestSharp;
using QuantConnect.Data;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Dynamic;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace QuantConnect.Lean.Engine.DataFeeds.Transport
{
    /// <summary>
    /// Represents a stream reader capable of polling a rest resource and queueing data
    /// </summary>
    public class RestFileSubscriptionStreamReader : IStreamReader
    {
        private readonly RestClient _client;
        private readonly RestRequest _request;
        private IRestResponse _response;
        private Queue<string> split;
        FileFormat _format;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestFileSubscriptionStreamReader"/> class.
        /// </summary>
        /// <param name="source">The source url to poll with a GET</param>
        /// <param name="format">The file format</param>
        public RestFileSubscriptionStreamReader(string source, FileFormat format)
        {
            _client = new RestClient(source);
            _client.Timeout = 240000;
            _request = new RestRequest(Method.GET);
            _format = format;
            if (_format == FileFormat.Csv)
            {
                _request.AddHeader("Accept", "text/csv");
            }
        }

        /// <summary>
        /// Gets <see cref="SubscriptionTransportMedium.Rest"/>
        /// </summary>
        public SubscriptionTransportMedium TransportMedium
        {
            get { return SubscriptionTransportMedium.Rest; }
        }

        /// <summary>
        /// Gets whether or not there's more data to be read in the stream
        /// </summary>
        public bool EndOfStream { get; set; }

        /// <summary>
        /// Gets the next line/batch of content from the stream 
        /// </summary>
        public string ReadLine()
        {
            try
            {
                if (_response == null)
                {
                    _response = _client.Execute(_request);
                    if (_response != null)
                    {
                        if (_format == FileFormat.Csv)
                        {
                            split = new Queue<string>(_response.Content.Split('\n'));
                        }
                        else if (_format == FileFormat.Json)
                        {

                            if (split == null)
                            {
                                var raw = JsonConvert.DeserializeObject<IEnumerable<JToken>>(_response.Content);
                                split = new Queue<string>(raw.Select(t => JsonConvert.SerializeObject(t)));
                            }
                            return split.Dequeue();

                        }
                        else
                        {
                            split = new Queue<string>(new[] { _response.Content });
                        }
                    }
                }
                if (split != null && split.Count > 0)
                {
                    return split.Dequeue();
                }
                else
                {
                    EndOfStream = true;
                }

            }
            catch (Exception err)
            {
                Log.Error(err);
            }

            return string.Empty;
        }

        /// <summary>
        /// This stream reader doesn't require disposal
        /// </summary>
        public void Dispose()
        {
        }
    }
}