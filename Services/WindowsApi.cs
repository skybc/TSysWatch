using System.Runtime.InteropServices;
using System.Security.Principal;

namespace TSysWatch.Services
{
    /// <summary>
    /// Windows API P/Invoke 声明
    /// </summary>
    public static class WindowsApi
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetProcessAffinityMask(IntPtr hProcess, IntPtr dwProcessAffinityMask);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetProcessAffinityMask(IntPtr hProcess, out IntPtr lpProcessAffinityMask, out IntPtr lpSystemAffinityMask);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges,
            ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();

        public const uint PROCESS_SET_INFORMATION = 0x0200;
        public const uint PROCESS_QUERY_INFORMATION = 0x0400;
        public const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        public const uint TOKEN_QUERY = 0x0008;
        public const string SE_DEBUG_NAME = "SeDebugPrivilege";

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            public LUID_AND_ATTRIBUTES Privileges;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public uint Attributes;
        }

        public const uint SE_PRIVILEGE_ENABLED = 0x00000002;
    }

    /// <summary>
    /// 权限管理器
    /// </summary>
    public static class PrivilegeManager
    {
        /// <summary>
        /// 检查是否具有管理员权限
        /// </summary>
        public static bool IsRunningAsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// 启用调试权限
        /// </summary>
        public static bool EnableDebugPrivilege()
        {
            try
            {
                IntPtr hToken;
                if (!WindowsApi.OpenProcessToken(WindowsApi.GetCurrentProcess(),
                    WindowsApi.TOKEN_ADJUST_PRIVILEGES | WindowsApi.TOKEN_QUERY, out hToken))
                {
                    return false;
                }

                WindowsApi.LUID luid;
                if (!WindowsApi.LookupPrivilegeValue(null, WindowsApi.SE_DEBUG_NAME, out luid))
                {
                    WindowsApi.CloseHandle(hToken);
                    return false;
                }

                WindowsApi.TOKEN_PRIVILEGES tokenPrivileges = new WindowsApi.TOKEN_PRIVILEGES();
                tokenPrivileges.PrivilegeCount = 1;
                tokenPrivileges.Privileges.Luid = luid;
                tokenPrivileges.Privileges.Attributes = WindowsApi.SE_PRIVILEGE_ENABLED;

                bool result = WindowsApi.AdjustTokenPrivileges(hToken, false, ref tokenPrivileges,
                    (uint)Marshal.SizeOf(typeof(WindowsApi.TOKEN_PRIVILEGES)), IntPtr.Zero, IntPtr.Zero);

                WindowsApi.CloseHandle(hToken);
                return result && Marshal.GetLastWin32Error() == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}