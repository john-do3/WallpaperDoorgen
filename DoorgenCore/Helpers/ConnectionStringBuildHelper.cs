using Doorgen.Core;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Core.EntityClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoorgenCore.Helpers
{
    class ConnectionStringBuildHelper
    {
        public static string Construct()
        {
            string result;

            DoorgenOptions options = DoorgenCoreClass.ReadConfig();

            string dbServer = options.dbServer;
            string dbUser = options.dbUser;
            string dbPassword = options.dbPassword;
            string dbName = options.dbName;
            string dbModelName = "wssModel";

            var entityConnectionBuilder = new EntityConnectionStringBuilder
            {

                Provider = "System.Data.SqlClient",
                ProviderConnectionString = string.Format("server={0};user id={1};password={2};persistsecurityinfo=True;database={3}",
                dbServer, dbUser, dbPassword, dbName),
                Metadata = string.Format(@"res://*/Models.{0}.csdl|res://*/Models.{0}.ssdl|res://*/Models.{0}.msl", dbModelName)

                // web.config values
                //metadata = res://*/Models.BalancerDataModel.csdl|res://*/Models.BalancerDataModel.ssdl|res://*/Models.BalancerDataModel.msl;
                //provider=MySql.Data.MySqlClient;
                //provider connection string=&quot;;server=localhost;user id=root;password=1111;persistsecurityinfo=True;database=brixxbalancer&quot;;"
                //providerName="System.Data.EntityClient"
            };

            result = entityConnectionBuilder.ToString();
            return result;
        }
    }
}
