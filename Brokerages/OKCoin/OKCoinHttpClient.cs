using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace QuantConnect.Brokerages.OKCoin
{

    public class OKCoinHttpClient : IOKCoinHttpClient
    {

        string _baseUrl;

        public OKCoinHttpClient(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public String Post(String url, Dictionary<String, String> args)
        {
            String content = "";
            HttpWebResponse response = null;
            StreamReader reader = null;
            try
            {
                HttpWebRequest quest = (HttpWebRequest)WebRequest.Create(_baseUrl + "/" + url);
                quest.Method = "POST";
                quest.ContentType = "application/x-www-form-urlencoded";

                StringBuilder buffer = new StringBuilder();
                foreach (string key in args.Keys)
                {
                    if (buffer.Length > 0)
                    {
                        buffer.AppendFormat("&{0}={1}", key, args[key]);
                    }
                    else
                    {
                        buffer.AppendFormat("{0}={1}", key, args[key]);
                    }
                }
                byte[] body = Encoding.UTF8.GetBytes(buffer.ToString());
                quest.ContentLength = body.Length;
                quest.GetRequestStream().Write(body, 0, body.Length);

                response = (HttpWebResponse)quest.GetResponse();
                reader = new StreamReader(response.GetResponseStream());
                content = reader.ReadToEnd();
                response.Close();
                reader.Close();
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
                if (reader != null)
                {
                    reader.Close();
                }
            }
            return content;
        }

    }
}
