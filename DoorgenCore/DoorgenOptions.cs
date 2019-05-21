using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Doorgen.Core
{
    public class DoorgenOptions
    {
        public string keywordsFile { get; set; }
        public string imagesOutputDir { get; set; }
        public string ruCaptchaApiKey { get; set; }

        public string dbServer { get; set; }
        public string dbUser { get; set; }
        public string dbPassword { get; set; }
        public string dbName { get; set; }

        public string cmsPath { get; set; }
    }
}
