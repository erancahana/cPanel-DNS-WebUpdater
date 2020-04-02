using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ByteSizeLib;
using Serilog;

namespace CpanelDdnsProvider {
    public class Logging {
        public static readonly ILogger Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                "log.txt",
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: (long)ByteSize.FromMegaBytes(5).Bytes,
                retainedFileCountLimit: 10,
                rollingInterval: RollingInterval.Infinite
            )
            .WriteTo.Trace()
            .CreateLogger();
    }
}
