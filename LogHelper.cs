using Serilog;
using Serilog.Core;

namespace TSysWatch
{
    public static class LogHelper
    {
        /// <summary>
        /// 构建日志路径
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static string GetLogPath()
        {
            // 获取本地目录
            string logDirectory = Path.Combine(Environment.CurrentDirectory, "Logs");

            return Path.Combine(logDirectory, $"log_.txt");

        }
        /// <summary>
        /// 创建日志对象
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        private static Logger CreateLogger()
        {
            return new LoggerConfiguration()
                .WriteTo.File(path: GetLogPath(),
                    fileSizeLimitBytes: 1024 * 1024 * 10,
                    retainedFileCountLimit: 500,
                    rollOnFileSizeLimit: true,
                    rollingInterval: RollingInterval.Day,
                    shared: true,
                    retainedFileTimeLimit: TimeSpan.FromDays(7),
                    flushToDiskInterval: TimeSpan.FromSeconds(1)
                    ).CreateLogger();
        }
        static ILogger logger = CreateLogger();
        public static ILogger Logger
        {
            get
            {
                return logger;
            }
        }
    }
}