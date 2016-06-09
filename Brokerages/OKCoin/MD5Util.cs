using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.OKCoin
{

    public class MD5Util
    {

        public static string BuildSign(Dictionary<string, string> data, string secretKey)
        {
            string mysign = "";
            try
            {
                string prestr = MD5Util.CreateLinkstring(data);
                prestr = (prestr + ("&secret_key=" + secretKey));

                mysign = GetMD5string(prestr);
            }
            catch (Exception ex)
            {

            }

            return mysign;
        }

        public static string CreateLinkstring(Dictionary<string, string> data)
        {
            List<string> keys = new List<string>(data.Keys.OrderBy(k => k));

            string prestr = "";
            for (int i = 0; (i < keys.Count); i++)
            {
                string key = keys[i];
                if ((i == (keys.Count - 1)))
                {
                    prestr = (prestr + (key + ("=" + data[key])));
                }
                else
                {
                    prestr = (prestr + (key + ("=" + (data[key] + "&"))));
                }

            }

            return prestr;
        }

        public static string GetMD5string(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return "";
            }

            byte[] bytes = str.GetBytes();
            HashAlgorithm md5 = HashAlgorithm.Create("MD5");
            byte[] hashed = md5.ComputeHash(bytes);

            return BitConverter.ToString(hashed).Replace("-", "").ToUpper();
        }


    }
}
