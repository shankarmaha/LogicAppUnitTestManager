using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicApp.AcceptanceTests.Utilities.LogicAppDiagnostics
{
    public class Config
    {
        public static string SubscriptionId
        {
            get
            {
                return ConfigurationManager.AppSettings["SubscriptionId"];
            }
        }

        public static string TenantId
        {
            get
            {
                return ConfigurationManager.AppSettings["TenantId"];
            }
        }

        public static string WebApiApplicationId
        {
            get
            {
                return ConfigurationManager.AppSettings["WebApiApplicationId"];
            }
        }

        public static string Secret
        {
            get
            {
                return ConfigurationManager.AppSettings["Secret"];
            }
        }

    }
}
