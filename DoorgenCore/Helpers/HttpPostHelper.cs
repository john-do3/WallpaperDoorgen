using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Doorgen.Core.Helpers
{
    internal static class HttpPostHelper
    {
        /// <summary>
        /// Post request to the specified url
        /// </summary>
        /// <param name="url">the url of handler</param>
        /// <param name="parameters">the parameters</param>
        /// <param name="result">true if everything is good, else false</param>
        /// <returns>Return string of that was returning from the handler</returns>
        public static string PostRequest(string url, Dictionary<string, string> parameters, out bool result)
        {
            Logger logger = LogManager.GetCurrentClassLogger();
            logger.Info("Отправка запроса " + url);

            var request = (HttpWebRequest)WebRequest.Create(url);

            request.KeepAlive = false;
            request.ContentType = "application/x-www-form-urlencoded";
            request.Method = "POST";

            var stringList = parameters.Select(item => String.Format("{0}={1}", item.Key, HttpUtility.UrlEncode(item.Value))).ToList();

            var content = String.Join("&", stringList);

            byte[] sentData = Encoding.GetEncoding("utf-8").GetBytes(content);
            request.ContentLength = sentData.Length;

            try
            {
                var sendStream = request.GetRequestStream();
                sendStream.Write(sentData, 0, sentData.Length);
                sendStream.Close();
            }
            catch (Exception exc)
            {
                logger.Error(exc, "Ошибка отправки запроса");
                result = false;
                return null;
            }

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    using (var stream = response.GetResponseStream())
                    {
                        using (var streamReader = new StreamReader(stream))
                        {
                            logger.Info(response.StatusCode.ToString());
                            if (response.StatusCode == HttpStatusCode.OK)
                            {
                                result = true;
                                var answerFromServer = streamReader.ReadToEnd();
                                logger.Info("Ответ сервера\n" + answerFromServer);
                                return answerFromServer;
                            }
                            else
                            {
                                result = false;
                                return null;
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                logger.Error(exc, $"Ошибка во время отправки запроса {exc.Message}");
                result = false;
                return null;
            }
        }
    }
}
