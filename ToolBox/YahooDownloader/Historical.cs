using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

namespace QuantConnect.ToolBox.YahooDownloader
{
    /// <summary>
    /// Class for fetching stock historical price from Yahoo Finance
    /// Copyright Dennis Lee
    /// 19 May 2017
    /// 
    /// </summary>
    public class Historical
    {

        /// <summary>
        /// Get stock historical price from Yahoo Finance
        /// </summary>
        /// <param name="symbol">Stock ticker symbol</param>
        /// <param name="start">Starting datetime</param>
        /// <param name="end">Ending datetime</param>
        /// <returns>List of history price</returns>
        public static List<HistoryPrice> Get(string symbol, DateTime start, DateTime end, string eventCode)
        {
            List<HistoryPrice> HistoryPrices = new List<HistoryPrice>();

            try
            {
                string csvData = GetRaw(symbol, start, end, eventCode);
                if (csvData != null)
                    HistoryPrices = Parse(csvData);
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }

            return HistoryPrices;

        }

        /// <summary>
        /// Get raw stock historical price from Yahoo Finance
        /// </summary>
        /// <param name="symbol">Stock ticker symbol</param>
        /// <param name="start">Starting datetime</param>
        /// <param name="end">Ending datetime</param>
        /// <returns>Raw history price string</returns>

        public static string GetRaw(string symbol, DateTime start, DateTime end, string eventCode)
        {

            string csvData = null;

            try
            {
                string url = "https://query1.finance.yahoo.com/v7/finance/download/{0}?period1={1}&period2={2}&interval=1d&events={3}&crumb={4}";

                //if no token found, refresh it
                if (string.IsNullOrEmpty(Token.Cookie) | string.IsNullOrEmpty(Token.Crumb))
                {
                    if (!Token.Refresh(symbol))
                        return GetRaw(symbol, start, end, eventCode);
                }

                url = string.Format(url, symbol, Math.Round(DateTimeToUnixTimestamp(start), 0), Math.Round(DateTimeToUnixTimestamp(end), 0), eventCode, Token.Crumb);

                using (WebClient wc = new WebClient())
                {
                    wc.Headers.Add(HttpRequestHeader.Cookie, Token.Cookie);
                    csvData = wc.DownloadString(url);
                }

            }
            catch (WebException webEx)
            {
                HttpWebResponse response = (HttpWebResponse)webEx.Response;

                //Re-fecthing token
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Debug.Print(webEx.Message);
                    Token.Cookie = "";
                    Token.Crumb = "";
                    Debug.Print("Re-fetch");
                    return GetRaw(symbol, start, end, eventCode);
                }
                else
                {
                    throw;
                }

            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }

            return csvData;

        }

        /// <summary>
        /// Parse raw historical price data into list
        /// </summary>
        /// <param name="csvData"></param>
        /// <returns></returns>
        private static List<HistoryPrice> Parse(string csvData)
        {

            List<HistoryPrice> hps = new List<HistoryPrice>();

            try
            {
                string[] rows = csvData.Split(Convert.ToChar(10));

                //row(0) was ignored because is column names 
                //data is read from oldest to latest
                for (int i = 1; i <= rows.Length - 1; i++)
                {

                    string row = rows[i];
                    if (string.IsNullOrEmpty(row))
                        continue;

                    string[] cols = row.Split(',');
                    if (cols[1] == "null")
                        continue;

                    HistoryPrice hp = new HistoryPrice();
                    hp.Date = DateTime.Parse(cols[0]);
                    hp.Open = Convert.ToDecimal(cols[1]);
                    hp.High = Convert.ToDecimal(cols[2]);
                    hp.Low = Convert.ToDecimal(cols[3]);
                    hp.Close = Convert.ToDecimal(cols[4]);
                    hp.AdjClose = Convert.ToDecimal(cols[5]);

                    //fixed issue in some currencies quote (e.g: SGDAUD=X)
                    if (cols[6] != "null")
                        hp.Volume = Convert.ToDecimal(cols[6]);

                    hps.Add(hp);

                }

            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }

            return hps;

        }

        #region Unix Timestamp Converter

        //credits to ScottCher
        //reference http://stackoverflow.com/questions/249760/how-to-convert-a-unix-timestamp-to-datetime-and-vice-versa
        private static DateTime UnixTimestampToDateTime(double unixTimeStamp)
        {
            //Unix timestamp Is seconds past epoch
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTimeStamp).ToLocalTime();
        }

        //credits to Dmitry Fedorkov
        //reference http://stackoverflow.com/questions/249760/how-to-convert-a-unix-timestamp-to-datetime-and-vice-versa
        private static double DateTimeToUnixTimestamp(DateTime dateTime)
        {
            //Unix timestamp Is seconds past epoch
            return (dateTime.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        #endregion

    }

    public class HistoryPrice
    {
        public DateTime Date { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
        public decimal AdjClose { get; set; }
    }
}