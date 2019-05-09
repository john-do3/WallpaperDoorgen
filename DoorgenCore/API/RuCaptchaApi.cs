using Doorgen.Core.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoorgenCore.Core.API
{
    public class RuCaptchaApi
    {
        public static string cId = "";
        public static string ruCaptchaApiKey = "09adec4cc142993a7cea6d798e03d30b";
        public static string ruCaptchaInUrl = "http://rucaptcha.com/in.php";
        public static string ruCaptchaResUrl = "http://rucaptcha.com/res.php?key={0}&action={1}&id={2}&json=1";

        public class RuClickCaptchaCoordinates
        {
            public string x { get; set; }
            public string y { get; set; }
        }

        public class RuCaptchaBasicResponse
        {
            public string status { get; set; }
        }

        public class RuCaptchaResponse: RuCaptchaBasicResponse
        {            
            public string request { get; set; }
        }

        public class RuClickCaptchaResponse: RuCaptchaBasicResponse
        {
            public List<RuClickCaptchaCoordinates> request { get; set; }
        }
    }
}
