using SqlSugar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TSysWatch
{
    internal class DbRepair
    {
        private static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DbRepair.ini");
        private static bool _isEnabled = false;
        private static bool _isRunning = false;

        public static void Start()
        {
            // 读取配置
            ReadIniFile();

            if (!_isEnabled)
            {
                LogHelper.Logger.Information("DbRepair功能已禁用");
                return;
            }

            LogHelper.Logger.Information("DbRepair功能已启用，开始运行");
            Task.Run(Run);
        }

        /// <summary>
        /// 读取INI配置文件
        /// </summary>
        private static void ReadIniFile()
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                {
                    CreateDefaultIniFile();
                    return;
                }

                var lines = File.ReadAllLines(ConfigFilePath, Encoding.UTF8);

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#") || trimmedLine.StartsWith(";"))
                        continue;

                    if (trimmedLine.Contains("="))
                    {
                        var parts = trimmedLine.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            var key = parts[0].Trim().ToLower();
                            var value = parts[1].Trim();

                            switch (key)
                            {
                                case "enabled":
                                    if (bool.TryParse(value, out bool enabled))
                                        _isEnabled = enabled;
                                    break;
                            }
                        }
                    }
                }

                LogHelper.Logger.Information($"读取DbRepair配置文件成功，启用状态：{_isEnabled}");
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"读取DbRepair配置文件异常：{ex.Message}", ex);
                // 如果读取配置失败，默认启用
                _isEnabled = true;
            }
        }

        /// <summary>
        /// 创建默认配置文件
        /// </summary>
        private static void CreateDefaultIniFile()
        {
            try
            {
                var defaultContent = @"# DbRepair数据库修复功能配置
# 是否启用数据库修复功能，true=启用，false=禁用
Enabled=true
";

                File.WriteAllText(ConfigFilePath, defaultContent, Encoding.UTF8);
                LogHelper.Logger.Information($"创建默认DbRepair配置文件：{ConfigFilePath}");

                // 默认启用
                _isEnabled = true;
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"创建默认DbRepair配置文件异常：{ex.Message}", ex);
                // 如果创建配置失败，默认启用
                _isEnabled = true;
            }
        }

        private static void Run()
        {
            _isRunning = true;
            while (_isRunning)
            {
                try
                {
                    using var db = CreateTestResultDb();
                    db.Ado.ExecuteCommand("select count(1) from t_dataitems");
                }
                catch (Exception ex)
                {
                    LogHelper.Logger.Error("数据库异常，" + ex.Message, ex);
                    if (ex.Message.Contains("repair"))
                    {
                        try
                        {
                            using var db = CreateTestResultDb();
                            // 修复数据表
                            db.Ado.ExecuteCommand("repair table t_dataitems");
                            LogHelper.Logger.Information("数据库修复操作已执行");
                        }
                        catch (Exception ex2)
                        {
                            // 修复失败
                            LogHelper.Logger.Error("数据库修复失败，" + ex2.Message, ex2);
                        }
                    }
                }
                finally
                {
                    Thread.Sleep(60 * 60 * 1000); // 每小时检查一次
                }
            }
        }

        /// <summary>
        /// 停止DbRepair
        /// </summary>
        public static void Stop()
        {
            _isRunning = false;
            LogHelper.Logger.Information("DbRepair功能停止");
        }


        /// <summary>
        /// 创建测试结果数据库
        /// </summary>
        /// <param name="dbPath"> </param>
        /// <returns> </returns>
        public static ISqlSugarClient CreateTestResultDb(string dbPath = "")
        {

            dbPath = "server=127.0.0.1;port=3306;user=root;password=123456;database=sgamma_result;AllowLoadLocalInfile=true";

            var sqlResultScope = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = dbPath,
                DbType = DbType.MySql,
                IsAutoCloseConnection = true //自动释放数据务，如果存在事务，在事务结束后释放
            });

            return sqlResultScope;
        }
    }
}
