using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace TSysWatch.Services
{
    /// <summary>
    /// 端口检测服务，用于检测端口是否被占用并找到可用端口
    /// </summary>
    public class PortDetectionService
    {
        private const int DefaultPort = 8002;
        private const int MinPort = 8003;
        private const int MaxPort = 8200;
        private const string PortLogDirectory = "Logs";
        private const string PortLogFileName = "port_detection.log";

        /// <summary>
        /// 获取可用的端口
        /// </summary>
        /// <returns>返回可用的端口号</returns>
        public int GetAvailablePort()
        {
            int selectedPort = DefaultPort;
            string reason = string.Empty;

            // 检查默认端口是否被占用
            if (IsPortInUse(DefaultPort))
            {
                // 默认端口被占用，寻找可用端口
                selectedPort = FindAvailablePort();
                reason = $"端口 {DefaultPort} 已被占用，自动选择端口 {selectedPort}";
            }
            else
            {
                reason = $"端口 {DefaultPort} 可用";
            }

            // 记录到文件
            LogPortSelection(selectedPort, reason);

            return selectedPort;
        }

        /// <summary>
        /// 检查指定端口是否被占用
        /// </summary>
        /// <param name="port">要检查的端口号</param>
        /// <returns>如果端口被占用返回 true，否则返回 false</returns>
        private bool IsPortInUse(int port)
        {
            try
            {
                IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                IPEndPoint[] tcpEndPoints = ipGlobalProperties.GetActiveTcpListeners();

                return tcpEndPoints.Any(endPoint => endPoint.Port == port);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查端口时出错: {ex.Message}");
                return true; // 如果检查失败，假设端口被占用以保险起见
            }
        }

        /// <summary>
        /// 查找可用的端口
        /// </summary>
        /// <returns>返回第一个可用的端口号</returns>
        private int FindAvailablePort()
        {
            for (int port = MinPort; port <= MaxPort; port++)
            {
                if (!IsPortInUse(port))
                {
                    return port;
                }
            }

            // 如果范围内没有可用端口，返回最后一个检查的端口
            return MaxPort;
        }

        /// <summary>
        /// 将端口选择信息记录到文件
        /// </summary>
        /// <param name="port">选择的端口号</param>
        /// <param name="reason">选择原因</param>
        private void LogPortSelection(int port, string reason)
        {
            try
            {
                // 确保日志目录存在
                if (!Directory.Exists(PortLogDirectory))
                {
                    Directory.CreateDirectory(PortLogDirectory);
                }

                string logFilePath = Path.Combine(PortLogDirectory, PortLogFileName);
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logMessage = $"[{timestamp}] {reason} | 最终使用端口: {port}";

                // 追加写入日志文件
                File.AppendAllText(logFilePath, logMessage + Environment.NewLine, Encoding.UTF8);

                Console.WriteLine($"[{timestamp}] {reason}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"记录端口信息失败: {ex.Message}");
            }
        }
    }
}
