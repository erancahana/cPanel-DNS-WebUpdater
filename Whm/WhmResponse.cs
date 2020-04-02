using System;
using LanguageExt;
using static LanguageExt.Prelude;

namespace CpanelDdnsProvider.Whm {
    class WhmResponse<T> {
        public T data { get; set; }
        public WhmResponseMetaData metadata { get; set; }

        public WhmResponse<T> VerifyResponseReason(string expected) =>
            VerifyResponseReason(reason =>
                reason.Equals(expected.Trim(), StringComparison.InvariantCultureIgnoreCase)
            );

        public WhmResponse<T> VerifyResponseReason(Func<string, bool> fExpected) {
            if (!fExpected(metadata.reason.Trim()))
                throw new WhmResponseException(this, $"Incorrect response metadata reason");

            return this;
        }
    }
}