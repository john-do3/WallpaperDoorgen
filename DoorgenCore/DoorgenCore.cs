using Doorgen.Core.API;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Doorgen.Core
{
    public class DoorgenCoreClass
    {
        DoorgenOptions options = null;
        Logger logger = LogManager.GetCurrentClassLogger();

        public string ExeDir
        {
            get
            {
                return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\";
            }
        }

        public void WriteTestConfig()
        {
            DoorgenOptions options = new DoorgenOptions()
            {
                keywordsFile = "keywords.txt",
                imagesOutputDir = "c:\\temp\\doorgen.output"
            };

            JObject o = (JObject)JToken.FromObject(options);

            using (StreamWriter file = System.IO.File.CreateText(@"config.json"))
            using (JsonTextWriter writer = new JsonTextWriter(file))
            {
                o.WriteTo(writer);
            }
        }

        public bool CheckConfigs()
        {
            bool configExist = System.IO.File.Exists(this.ExeDir + "config.json");

            return configExist;
        }

        public void ReadConfig()
        {
            using (TextReader reader = System.IO.File.OpenText(this.ExeDir + "config.json"))
            {
                string opt = reader.ReadToEnd();
                options = JsonConvert.DeserializeObject<DoorgenOptions>(opt);
            }            
        }

        public int Process()
        {
            if (this.options == null)
                return -1;

            using (TextReader reader = System.IO.File.OpenText(this.options.keywordsFile))
            {

                string allKeywords = reader.ReadToEnd();
                string[] keywordsArray = allKeywords.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                // todo some session statistics
                // - total images processed
                // - captchas resolved
                // - captchas failed 
                // - some else
                foreach (string keywords in keywordsArray)
                {
                    try
                    {
                        QwantApi.ProcessSearch(keywords, this.options.imagesOutputDir);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex.Message);
                    }
                }

            }        

            return 0;
        }
    }
}
