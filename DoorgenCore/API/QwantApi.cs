using Doorgen.Core.Helpers;
using DoorgenCore.Core.API;
using DoorgenCore.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using static DoorgenCore.Core.API.RuCaptchaApi;

namespace Doorgen.Core.API
{
    public class QwantApi
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        string processedImagesDir;

        private DoorgenOptions options;
        private ImagesImportHelper imagesImportHelper;

        public QwantApi(DoorgenOptions options)
        {
            this.options = options;            
        }

        public class QwantImage
        {
            public int width { get; set; }
            public int height { get; set; }
            // image url
            public string media { get; set; }
            public string keywords { get; set; }
            public string title { get; set; }
            public string file { get; set; }
        }

        public class QwantAntiRobotData
        {
            public string id { get; set; }
            public string img { get; set; } // based 64 image
        }

        public class QwantAntiRobotResponse
        {
            public string status { get; set; }
            public QwantAntiRobotData data { get; set; }
        }

        public class QwantAntiRobotResolveResponse
        {
            public string status { get; set; }
            public int error { get; set; }
        }

        public string SearchImages(string keywords)
        {
            // anime%20manga%20wallpaper            

            int imageQnt = 5;
            string url = $"https://api.qwant.com/api/search/images?count={imageQnt}&q={keywords}&t=images&safesearch=1&locale=en_US&uiv=4";

            keywords = keywords.Replace(" ", "%20");
            string result = HttpGetHelper.HttpGet(url);
            return result;

            /*HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
            var result = httpClient.GetAsync(url).Result;

            return result.Content.ToString();*/
        }

        internal bool GetAntiRobot(string outputPath)
        {
            string qwantCaptchaId = string.Empty;
            string ruCaptchaId = string.Empty;
            string url = "https://api.qwant.com/api/anti_robot/get";            

            string response = HttpGetHelper.HttpGet(url);
            QwantAntiRobotResponse result = JsonConvert.DeserializeObject<QwantAntiRobotResponse>(response);

            if (result.status == "success")
            {
                qwantCaptchaId = result.data.id;
                /* sample code for saving captcha image
                 * 
                
                string base64str = result.data.img.Split(',')[1];
                byte[] bytes = Convert.FromBase64String(base64str);

                Image image;
                using (MemoryStream ms = new MemoryStream(bytes))
                {
                    image = Image.FromStream(ms);
                }

                Bitmap img = new Bitmap(image);
                img.Save($"{outputPath}\\antiRobot.png");*/


                var parameters = new Dictionary<string, string>() {
                    { "coordinatescaptcha", "1" },
                    { "method", "base64"},
                    { "key", this.options.ruCaptchaApiKey },
                    { "body", result.data.img },
                    { "json", "1" },
                    { "textinstructions", "Найдите фигуру, отличающуюся от остальных" }
                };

                bool bResult = false;
                var postResult = HttpPostHelper.PostRequest(RuCaptchaApi.ruCaptchaInUrl, parameters, out bResult);

                if (bResult)
                {
                    bResult = false;
                    var captchaInResult = JsonConvert.DeserializeObject<RuCaptchaApi.RuCaptchaResponse>(postResult);

                    if (captchaInResult.status != "1")
                    {
                        logger.Warn($"RuCaptcha/in.php post request error {captchaInResult.request}");
                    }
                    else
                    {
                        // captcha post request successfull, try get result
                        ruCaptchaId = captchaInResult.request;
                        bool captchaRecognized = false;
                        int errorsCount = 0;

                        do
                        {
                            Thread.Sleep(5000);

                            string resQuery = string.Format(RuCaptchaApi.ruCaptchaResUrl, 
                                this.options.ruCaptchaApiKey, 
                                "get",
                                ruCaptchaId
                                );

                            response = HttpGetHelper.HttpGet(resQuery);
                            RuCaptchaApi.RuCaptchaBasicResponse captchaGetResult;

                            try
                            {
                                captchaGetResult = JsonConvert.DeserializeObject<RuCaptchaApi.RuCaptchaResponse>(response);
                                captchaRecognized = captchaGetResult.status == "1";

                                if (!captchaRecognized)
                                {
                                    logger.Warn($"RuCaptcha/res.php get result error {(captchaGetResult as RuCaptchaApi.RuCaptchaResponse).request}");
                                    errorsCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                captchaGetResult = JsonConvert.DeserializeObject<RuCaptchaApi.RuClickCaptchaResponse>(response);
                                captchaRecognized = captchaGetResult.status == "1";

                                if (!captchaRecognized)
                                {
                                    logger.Warn($"RuCaptcha/res.php get result unknown error");
                                    errorsCount++;
                                }
                            }
                            
                            if (captchaRecognized)
                            {
                                var captchaCoordinatesResult = JsonConvert.DeserializeObject<RuCaptchaApi.RuClickCaptchaResponse>(response);
                                //var coordinates = captchaCoordinatesResult.request[0];

                                // send results to QwantApi
                                bResult = this.ResolveAntiRobot(qwantCaptchaId, ruCaptchaId, captchaCoordinatesResult.request);                                
                            }

                            Thread.Sleep(5000);
                        }
                        while (!captchaRecognized && (errorsCount < 12));
                    }
                }

                return bResult;
            }

            return false;
        }

        internal bool ResolveAntiRobot(string qwantCaptchaId, string ruCaptchaId, List<RuClickCaptchaCoordinates> coordinatesList)
        {
            bool result = false;
            string url = "https://api.qwant.com/api/anti_robot/resolve";
            string resQuery = string.Empty;

            foreach (RuClickCaptchaCoordinates coords in coordinatesList)
            {
                var parameters = new Dictionary<string, string>()
                {
                    { "id", qwantCaptchaId},
                    { "x", coords.x},
                    { "y", coords.y }
                };

                // post to QwantAPI robot resolve
                bool bResult = false;
                var postResult = HttpPostHelper.PostRequest(url, parameters, out bResult);

                if (bResult)
                {
                    QwantAntiRobotResolveResponse response = JsonConvert.DeserializeObject<QwantAntiRobotResolveResponse>(postResult);

                    if (response.status == "success")
                    {
                        // send reportgood
                        logger.Warn("Sending 'reportgood' to ruCaptcha");
                        resQuery = string.Format(RuCaptchaApi.ruCaptchaResUrl,
                                this.options.ruCaptchaApiKey,
                                "reportgood",
                                ruCaptchaId
                                );                        

                        result = true;
                        break;
                    }
                }
                
                Thread.Sleep(3000);
            }

            if (!result)
            {
                logger.Error("Sending 'reportbad' to ruCaptcha");
                // send reportbad
                resQuery = string.Format(RuCaptchaApi.ruCaptchaResUrl,
                                this.options.ruCaptchaApiKey,
                                "reportbad",
                                ruCaptchaId
                                );
            }


            var getResponse = HttpGetHelper.HttpGet(resQuery);
            return result;
        }

        public static bool WebFileExist(string url)
        {
            bool exist = false;
            try
            {
                HttpWebRequest request = (HttpWebRequest)System.Net.WebRequest.Create(url);
                request.Timeout = 2000;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    exist = response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch
            {
            }

            return exist;
        }

        private bool PromptConfirmation(string confirmText)
        {
            Console.Write(confirmText + " [y/n] : ");
            ConsoleKey response = Console.ReadKey(false).Key;
            Console.WriteLine();
            return (response == ConsoleKey.Y);
        }

        public bool Init()
        {
            bool result = false;

            this.processedImagesDir = $"{DoorgenCoreClass.ExeDir}\\processed_images";
            if (!Directory.Exists(this.processedImagesDir))
                Directory.CreateDirectory(this.processedImagesDir);
            
            try
            {
                logger.Info($"Соединяемся с базой данных");
                this.imagesImportHelper = new ImagesImportHelper();

                if (PromptConfirmation("Хотите начать парсинг с нуля? (база данных и все ранее скачанные картинки будут очищены) "))
                {
                    logger.Info($"Очистка каталога с картинками");
                    // purge processedImagesDir directory
                    Directory.Delete(this.processedImagesDir, true);
                    Directory.CreateDirectory(this.processedImagesDir);

                    logger.Info($"Инициализация базы данных");
                    imagesImportHelper.InitCategories();
                }

                imagesImportHelper.ReadCategories();

                result = true;
            }
            catch (Exception ex)
            {
                logger.Error($"- init error '{ex.Message}'");
            }

            return result;
        }

        public void ProcessSearch(string keywords, string outputPath)
        {
            try
            {                

                logger.Info($"Обработка ключевой фразы '{keywords}'");                

                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);

                //string imageDir = $"{outputPath}\\{keywords}";
                // image path formed avCMS way - {first image name letter}\image name.ext
                string imageDir = $"{this.processedImagesDir}\\{keywords}";

                if (Directory.Exists(imageDir))
                {
                    if (Directory.GetFiles(imageDir).Length > 0)
                    {
                        logger.Warn($"- уже обработано");
                        return;
                    }
                }

                logger.Info($"- поиск картинок ->");
                string sQwantResponse = this.SearchImages(keywords);
                JObject jQwantResponse = JObject.Parse(sQwantResponse);

                var items = jQwantResponse["data"]["result"]["items"];
                List<QwantImage> qImages = JsonConvert.DeserializeObject<List<QwantImage>>(items.ToString());
                
                qImages = qImages.OrderByDescending(o => o.width).ToList();
                foreach (QwantImage image in qImages)
                {                    
                    logger.Info($"- проверка {image.media}");
                    if (QwantApi.WebFileExist(image.media))
                    {
                        if (!Directory.Exists(imageDir))
                            Directory.CreateDirectory(imageDir);

                        Uri uri = new Uri(image.media);
                        string localFileName = System.IO.Path.GetFileName(uri.LocalPath);

                        WebClient webClient = new WebClient();
                        {
                            try
                            {
                                logger.Info($"- загрузка..");
                                string fileName = $"{imageDir}\\{localFileName}";

                                webClient.DownloadFile(image.media, fileName);
                                logger.Info($"- успешно загружено");

                                // image post processing
                                fileName = ImagePostProcessHelper.ImageProcessorRotateAutoCrop(fileName, 5, image);

                                // copy image to output (CMS) directory
                                string outputImageDir = $"{outputPath}\\{localFileName[0]}";
                                if (!Directory.Exists(outputImageDir))
                                    Directory.CreateDirectory(outputImageDir);

                                string outputFileName = $"{outputImageDir}\\{localFileName}";
                                File.Copy(fileName, outputFileName);

                                // Import image into database
                                image.keywords = keywords;
                                image.file = $"{localFileName[0]}/{localFileName}";
                                this.imagesImportHelper.ImportImage(image);
                                
                                break; // images iteration
                            }
                            catch (WebException ex)
                            {
                                logger.Error($"- download error '{ex.Message}'");
                            }
                        }
                    }
                    else
                        logger.Error($"- does not exist");
                }
                
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("429"))
                {
                    logger.Error($"QwantAPI anti-robot raised. Trying to automatic resolve");

                    if (!this.GetAntiRobot(outputPath))
                    {
                        logger.Error("Automatic captcha resolve failed.");
                        logger.Error("\r\nPlease press Enter to continue");
                        Console.ReadLine();
                    }
                    else
                        logger.Warn("QwantAPI captcha solved, continue processing");
                }
                else
                    logger.Error($"- search error '{ex.Message}'");
            }
        }
    }

}
