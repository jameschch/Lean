using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace com.okcoin.rest
{

    public class HttpUtilManager
    {
        private HttpUtilManager() { }
        private static HttpUtilManager instance = new HttpUtilManager();
        public static HttpUtilManager getInstance()
        {
            return instance;
        }

        public String requestHttpPost(String url_prex, String url, Dictionary<String, String> paras)
        {
            String responseContent = "";
            HttpWebResponse httpWebResponse = null;
            StreamReader streamReader = null;
            try
            {
                url = url_prex + url;
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.Method = "POST";
                httpWebRequest.ContentType = "application/x-www-form-urlencoded";

                StringBuilder buffer = new StringBuilder();
                foreach (string key in paras.Keys)
                {
                    if (buffer.Length > 0)
                    {
                        buffer.AppendFormat("&{0}={1}", key, paras[key]);
                    }
                    else
                    {
                        buffer.AppendFormat("{0}={1}", key, paras[key]);
                    }
                }
                byte[] btBodys = Encoding.UTF8.GetBytes(buffer.ToString());
                httpWebRequest.ContentLength = btBodys.Length;
                httpWebRequest.GetRequestStream().Write(btBodys, 0, btBodys.Length);

                httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                streamReader = new StreamReader(httpWebResponse.GetResponseStream());
                responseContent = streamReader.ReadToEnd();
                httpWebResponse.Close();
                streamReader.Close();
                if (string.IsNullOrEmpty(responseContent))
                {
                    return "";
                }
            }
            finally
            {
                if (httpWebResponse != null)
                {
                    httpWebResponse.Close();
                }
                if (streamReader != null)
                {
                    streamReader.Close();
                }
            }
            return responseContent;
        }

    }
}
