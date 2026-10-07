using BobsBookstoreClassic.Data;
using Bookstore.Common;
using NLog;
using NLog.AWS.Logger;
using NLog.Config;
using NLog.Targets;

namespace Bookstore.Web
{
    public static class LoggingSetup
    {
        /// <summary>
        /// Creates the NLog logging configuration used at startup.
        /// NLog is configured as the logging provider for Microsoft.Extensions.Logging.
        /// </summary>
        public static LoggingConfiguration CreateNLogConfiguration()
        {
            var config = new LoggingConfiguration();

            Target loggingTarget;

            if (BookstoreConfiguration.GetSetting("Services/LoggingService") == "aws")
            {
                loggingTarget = new AWSTarget { LogGroup = Constants.AppName };
            }
            else
            {
                loggingTarget = new DebuggerTarget("debugger");
            }

            config.AddTarget(loggingTarget);
            config.LoggingRules.Add(new LoggingRule("*", NLog.LogLevel.Info, loggingTarget));

            return config;
        }
    }
}
