using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Flurl;
using Flurl.Http;
using LanguageExt;
using static LanguageExt.Prelude;
using StringExtensions;

namespace CpanelDdnsProvider.Whm {
    public class WhmDnsUpdater {

        readonly string _whmHost;
        readonly int _whmHostPort;
        readonly string _username;
        readonly string _securityToken;

        const int ApiVersion = 1;

        public WhmDnsUpdater(string whmHost, int whmHostPort, string username, string securityToken) {
            this._whmHost = whmHost;
            this._whmHostPort = whmHostPort;
            this._username = username;
            this._securityToken = securityToken;
        }

        const int Ttl_Default = 14400;

        public bool Update(string fromHost, IPAddress toIp, int? ttl = Ttl_Default) {
            ttl = ttl ?? Ttl_Default;
            try {
                fromHost = fromHost.Trim().Trim('.');
                if (!Regex.IsMatch(fromHost, ".+\\..+"))
                    throw new Exception("Domain to update is not a valid subdomain");

                var domainInfo = GetDomainInfo(fromHost);

                if (domainInfo.Domain.Trim('.') == fromHost) throw new Exception("Cannot change top level domain");

                var subdomainToUpdate = fromHost.TrimEnd(domainInfo.Domain).Trim('.');

                AddOrUpdate(domainInfo, subdomainToUpdate, toIp, ttl.Value);
                AddOrUpdate(domainInfo, $"*.{subdomainToUpdate}", toIp, ttl.Value);

                Logging.Logger.Information("Updated '{FromHost}' to '{ToIp}'", fromHost, toIp);
                return true;
            } catch (Exception ex) {
                Logging.Logger.Error(ex, "Failed to update '{FromHost}' to '{ToIp}'", fromHost, toIp);
                return false;
            }   
        }

        DomainInfo GetDomainInfo(string domainToUpdate) {
            var zone = GetMatchingZone(domainToUpdate);
            IEnumerable<dynamic> zoneRecords = GetZoneRecords(zone.domain);
            return new DomainInfo(zone.domain, zoneRecords);
        }

        IEnumerable<dynamic> GetZoneRecords(string domain) {
            var response = Call<dynamic>("dumpzone", ("domain", domain))
                .VerifyResponseReason("Zone Serialized");

            IEnumerable<dynamic> zones = response.data.zone;
            var zone = zones.Single();
            IEnumerable<dynamic> record = zone.record;
            return record.ToList();
        }

        void AddOrUpdate(DomainInfo domainInfo, string subdomainToUpdate, IPAddress realIp, int recordTtl) {
            IEnumerable<(string, object)> commandParams = new (string, object)[] {
                ("domain", domainInfo.Domain),
                ("name", subdomainToUpdate),
                ("class", "IN"),
                ("ttl", recordTtl),
                ("type", "A"),
                ("address", realIp.ToString()),
            };

            var matchingRecords = domainInfo.FindMatchingSubRecords(subdomainToUpdate);

            Func<string,bool> fVerifyDnsResponseReason = reason => 
                reason.StartsWith("Bind reloading", StringComparison.InvariantCultureIgnoreCase);

            if (!matchingRecords.Any()) {
                Call<Unit>("addzonerecord", commandParams)
                    .VerifyResponseReason(fVerifyDnsResponseReason);

            } else {
                var toUpdate = matchingRecords.First();
                int toUpdate_LineNum = toUpdate.Line;
                commandParams = commandParams.Append(("line", toUpdate_LineNum));
                Call<Unit>("editzonerecord", commandParams)
                    .VerifyResponseReason(fVerifyDnsResponseReason);

                var toRemoves = matchingRecords.Skip(1);
                foreach (var toRemove in toRemoves) {
                    int toRemove_LineNum = toRemove.Line;
                    Call<Unit>("removezonerecord", ("zone", domainInfo.Domain), ("line", toRemove_LineNum))
                        .VerifyResponseReason(fVerifyDnsResponseReason);
                    ;
                }
            }
        }

        (string domain, string zonefile) GetMatchingZone(string domainToUpdate) {
            var result = Call<dynamic>("listzones").VerifyResponseReason("OK");
            var zones = (result.data.zone as IEnumerable<dynamic>).Select(x => {
                string domain = x.domain;
                string zonefile = x.zonefile;

                return (domain, zonefile);
            }).ToList();

            var matchingZones = zones.Where(zone => domainToUpdate.EndsWith(zone.domain)).ToList();
            if (!matchingZones.Any()) throw new Exception($"No matching zones for requested domain {domainToUpdate}");
            if (matchingZones.Count() > 1) throw new Exception($"Multiple matching zones for requested domain {domainToUpdate}");
            return matchingZones.Single();
        }

        WhmResponse<T> Call<T>(string command, params (string name, object val)[] commandParams) =>
            Call<T>(command, commandParams.AsEnumerable());

        WhmResponse<T> Call<T>(string command, IEnumerable<(string name, object val)> commandParams) {
            var url = CreateCall(command, commandParams);
            
            var response = url.GetJsonAsync<WhmResponse<T>>().Result;

            if (response.metadata.command != command) throw new WhmResponseException(response, "Return metadata command is different from requested command");
            if (response.metadata.result != 1) throw new WhmResponseException(response, $"Request returned a failure result: {response.metadata.reason}");

            return response;
        }

        IFlurlRequest CreateCall(string command, IEnumerable<(string name, object val)> commandParams) {
            var url = new Url(new UriBuilder("https", _whmHost, _whmHostPort).Uri)
                    .WithHeader("Authorization", $"whm {_username}:{_securityToken}")
                    // .AppendPathSegment($"cpsess{securityToken}")
                    .AppendPathSegment("json-api")
                    .AppendPathSegment(command)
                    .SetQueryParam("api.version", ApiVersion)
                // .SetQueryParam("user", username)
                ;

            foreach (var commandParam in commandParams) {
                url.SetQueryParam(commandParam.name, commandParam.val);
            }

            return url;
        }
    }
}
