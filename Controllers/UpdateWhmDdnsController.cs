using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using CommonTools.Extensions;
using CpanelDdnsProvider.Whm;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace CpanelDdnsProvider.Controllers
{
    [Route("api/[controller]")]
    [Route("api/whmdns/update")]
    [ApiController]
    public class UpdateWhmDdnsController : ControllerBase {

        private readonly IOptions<CpanelDdnsUpdaterConfig> config;
        public UpdateWhmDdnsController(IOptions<CpanelDdnsUpdaterConfig> config) {
            this.config = config;
        }

        [HttpGet]
        public ActionResult<string> Get() {
            return "Up";
        }

        [HttpGet("log")]
        public ActionResult<string> Log() {
            
            Logging.Logger.Debug("Rcvd log request to {Url}", Request.GetDisplayUrl());
            return "OK";
        }

        [HttpGet("auth/{authCode}/from/{fromHost}/to/{toIp}")]
        [HttpGet("auth/{authCode}/from/{fromHost}/to/{toIp}/ttl/{ttl:int?}")]
        public ActionResult<string> Get(string authCode, string fromHost, string toIp, int? ttl = null) {
            IPAddress ip;
            try {
                if (!config.Value.RequestAuthCode.IsNullOrWhiteSpace()) {
                    if (authCode.Trim() != config.Value.RequestAuthCode.Trim())
                        throw new Exception("Unauthorized");
                }

                ip = ParseIpAddress(toIp);
                if (!Regex.IsMatch(fromHost, @".+\..+")) throw new Exception("WhmHost is invalid");
                const int ttl_min = 300;
                if (ttl.HasValue && ttl.Value < ttl_min) throw new Exception($"TTL is invalid, set to at least {ttl_min}");
            } catch (Exception ex) {
                return $"FAIL - {ex.Message}";
            }

            try {
                var result = new WhmDnsUpdater(config.Value.WhmHost, config.Value.WhmPort, config.Value.WhmUsername, config.Value.WhmSecurityToken)
                    .Update(fromHost, ip, ttl);

                return result 
                    ? $"OK updated '{fromHost}' to '{toIp}'"
                    : "FAIL";
            } catch (Exception ex) {
                Logging.Logger.Error(ex, "Unexpected error updating '{FromHost}' to '{ToIp}'", fromHost, toIp);
                return "FAIL - internal error";
            }
        }

        IPAddress ParseIpAddress(string ipStr) {
            try {
                return IPAddress.Parse(ipStr);
            } catch (ArgumentNullException argumentNullException) {
                throw new Exception("IP address is required");
            } catch (FormatException formatException) {
                throw new Exception("IP address is invalid");
            }
        }
    }
}