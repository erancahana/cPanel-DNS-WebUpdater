using CommonTools.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CpanelDdnsProvider.Whm {
    public class DomainInfo {
        public string Domain { get; }
        public IEnumerable<dynamic> Records { get; }

        public DomainInfo(string domain, IEnumerable<dynamic> records) {
            Domain = domain;
            Records = records;
        }

        public IEnumerable<dynamic> FindMatchingSubRecords(string subdomain) {
            var subDomain_variations = new[] {
                subdomain,
                $"{subdomain}.{Domain}",
            };

            return Records
                .Where(record => {
                    string recordName = record.name;

                    if (recordName.IsNullOrWhiteSpace()) return false;

                    return subDomain_variations.Any(sub =>
                        sub.Trim('.').Equals(recordName.Trim('.'), StringComparison.InvariantCultureIgnoreCase));
                })
                .ToList();
        }
    }
}