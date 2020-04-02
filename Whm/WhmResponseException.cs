using System;

namespace CpanelDdnsProvider.Whm {
    class WhmResponseException : Exception {
        public dynamic Response { get; }

        public WhmResponseException(dynamic response, string msg) : base(msg) {
            Response = response;
        }
    }
}