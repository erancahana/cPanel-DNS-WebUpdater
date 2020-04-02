using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using CpanelDdnsProvider.Whm;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CpanelDdnsProvider.Tests {
    public class TestUpdateDns {

        IConfiguration Configuration { get; set; }

        public TestUpdateDns() {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets(typeof(Startup).GetTypeInfo().Assembly);

            Configuration = builder.Build();
        }

        public CpanelDdnsUpdaterConfig Cfg() => Configuration.GetSection("CpanelDdnsUpdaterConfig").Get<CpanelDdnsUpdaterConfig>();


        public string HostnameToUpdate => "myDynamicSubdomain.myDomain.com";
        public IPAddress IpToUpdate => IPAddress.Parse("8.8.8.8");


        [Fact]
        void Test() {
            try {
                var cfg = Cfg();

                var result = new WhmDnsUpdater(cfg.WhmHost, cfg.WhmPort, cfg.WhmUsername, cfg.WhmSecurityToken)
                    .Update(HostnameToUpdate, IpToUpdate, 3600);

                Assert.True(result);

            } catch (Exception ex) {
                throw ex;

            }
        }
    }
}
