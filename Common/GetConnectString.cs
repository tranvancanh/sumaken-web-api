using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace WarehouseWebApi.common
{
    public class GetMasterConnectString
    {
        public string ConnectionString { get; set; }

        public GetMasterConnectString()
        {
            var databaseName = "master";
#if DEBUG
            databaseName = "master";
#endif
            var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false);
            var configuration = builder.Build();
            ConnectionString = configuration.GetSection("connectionString").GetValue<string>(databaseName);
        }

    }
    public class GetConnectString
    {
        public string ConnectionString { get; set; }

        public GetConnectString(string databaseName)
        {
            var connectionString1 = "warehouse1";
            var connectionString2 = "warehouse2";

            var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false);
            var configuration = builder.Build();
            ConnectionString = configuration.GetSection("connectionString").GetValue<string>(connectionString1) + databaseName + configuration.GetSection("connectionString").GetValue<string>(connectionString2);

        }
    }
}
