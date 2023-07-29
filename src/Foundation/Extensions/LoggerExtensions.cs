using Vintagestory.API.Common;

namespace Foundation.Extensions
{
    public static class LoggerExtensions
    {
        public static void LogRaw(this ILogger logger, EnumLogType logType, string message)
        {
            logger.Log(logType, "{0}", message);
        }
    }
}
