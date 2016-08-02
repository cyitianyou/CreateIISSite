using Microsoft.Web.Administration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateIISSite
{
    public static class ConfigurationExtensions
    {
        ///<summary>依据连接串名字connectionName返回数据连接字符串</summary>
        ///<param name="connectionName">连接串的</param>
        ///<param name="config"></param>
        ///<returns></returns>
        public static string GetConnectionStringsConfig(this Configuration config, string connectionName)
        {
           // string connectionString = config.ConnectionStrings.ConnectionStrings[connectionName].ConnectionString;
            ////Console.WriteLine(connectionString);
            return connectionString;

        }
    }
}
