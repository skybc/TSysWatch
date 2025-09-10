#include "pch.h"
#include "CpuCoreManager.h"
#include <iostream>
#include <thread>
#include <chrono>
#include <shellapi.h>

namespace SamsunIoCardC {
    namespace CpuManager {

        // =============================================================================
        // CpuCoreManager 类实现
        // =============================================================================

        CpuCoreManager::CpuCoreManager()
            : m_isProtectionActive(false)
            , m_protectedCore(0)
            , m_protectionThread(nullptr)
        {
        }

        CpuCoreManager::~CpuCoreManager() {
            StopCoreProtection();
        }

        bool CpuCoreManager::SetProcessAffinity(DWORD_PTR affinityMask) {
            HANDLE hProcess = GetCurrentProcess();

            if (!SetProcessAffinityMask(hProcess, affinityMask)) {
                std::cerr << "设置CPU亲和性失败，错误码: " << GetLastError() << std::endl;
                return false;
            }

            std::cout << "成功设置进程CPU亲和性掩码: 0x" << std::hex << affinityMask << std::dec << std::endl;
            return true;
        }

        DWORD_PTR CpuCoreManager::GetProcessAffinity() {
            HANDLE hProcess = GetCurrentProcess();
            DWORD_PTR processAffinityMask = 0;
            DWORD_PTR systemAffinityMask = 0;

            if (!GetProcessAffinityMask(hProcess, &processAffinityMask, &systemAffinityMask)) {
                std::cerr << "获取CPU亲和性失败，错误码: " << GetLastError() << std::endl;
                return 0;
            }

            return processAffinityMask;
        }

        bool CpuCoreManager::SetProcessPriorityLevel(DWORD priorityClass) {
            HANDLE hProcess = GetCurrentProcess();

            if (!SetPriorityClass(hProcess, priorityClass)) {
                std::cerr << "设置进程优先级失败，错误码: " << GetLastError() << std::endl;
                return false;
            }

            std::cout << "成功设置进程优先级: " << priorityClass << std::endl;
            return true;
        }

        void CpuCoreManager::DisplayCpuCoreInfo() {
            SYSTEM_INFO sysInfo = GetSystemInfo();

            std::cout << "=== CPU系统信息 ===" << std::endl;
            std::cout << "CPU核心数: " << sysInfo.dwNumberOfProcessors << std::endl;
            std::cout << "活动处理器掩码: 0x" << std::hex << sysInfo.dwActiveProcessorMask << std::dec << std::endl;

            std::cout << "可用CPU核心: ";
            for (DWORD i = 0; i < sysInfo.dwNumberOfProcessors; i++) {
                if (sysInfo.dwActiveProcessorMask & (1ULL << i)) {
                    std::cout << i << " ";
                }
            }
            std::cout << std::endl;
        }

        bool CpuCoreManager::BindToSingleCore(DWORD coreIndex) {
            SYSTEM_INFO sysInfo = GetSystemInfo();

            if (coreIndex >= sysInfo.dwNumberOfProcessors) {
                std::cerr << "错误: 核心索引 " << coreIndex << " 超出范围 (0-"
                    << (sysInfo.dwNumberOfProcessors - 1) << ")" << std::endl;
                return false;
            }

            DWORD_PTR affinityMask = 1ULL << coreIndex;

            if (!(sysInfo.dwActiveProcessorMask & affinityMask)) {
                std::cerr << "错误: CPU核心 " << coreIndex << " 不可用" << std::endl;
                return false;
            }

            return SetProcessAffinity(affinityMask);
        }

        bool CpuCoreManager::BindToMultipleCores(const std::vector<DWORD>& coreIndices) {
            SYSTEM_INFO sysInfo = GetSystemInfo();
            DWORD_PTR affinityMask = 0;

            for (DWORD coreIndex : coreIndices) {
                if (coreIndex >= sysInfo.dwNumberOfProcessors) {
                    std::cerr << "错误: 核心索引 " << coreIndex << " 超出范围" << std::endl;
                    return false;
                }

                DWORD_PTR coreMask = 1ULL << coreIndex;
                if (!(sysInfo.dwActiveProcessorMask & coreMask)) {
                    std::cerr << "错误: CPU核心 " << coreIndex << " 不可用" << std::endl;
                    return false;
                }

                affinityMask |= coreMask;
            }

            return SetProcessAffinity(affinityMask);
        }

        DWORD_PTR CpuCoreManager::GetProcessAffinityByPID(DWORD processId) {
            HANDLE hProcess = OpenProcess(PROCESS_QUERY_INFORMATION, FALSE, processId);
            if (hProcess == NULL) {
                return 0;
            }

            DWORD_PTR processAffinityMask = 0;
            DWORD_PTR systemAffinityMask = 0;

            if (!GetProcessAffinityMask(hProcess, &processAffinityMask, &systemAffinityMask)) {
                CloseHandle(hProcess);
                return 0;
            }

            CloseHandle(hProcess);
            return processAffinityMask;
        }

        std::map<DWORD, DWORD_PTR> CpuCoreManager::GetAllProcessesAffinity() {
            std::map<DWORD, DWORD_PTR> processAffinityMap;

            HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
            if (hSnapshot == INVALID_HANDLE_VALUE) {
                std::cerr << "创建进程快照失败，错误码: " << GetLastError() << std::endl;
                return processAffinityMap;
            }

            PROCESSENTRY32 pe32;
            pe32.dwSize = sizeof(PROCESSENTRY32);

            if (!Process32First(hSnapshot, &pe32)) {
                std::cerr << "遍历进程失败，错误码: " << GetLastError() << std::endl;
                CloseHandle(hSnapshot);
                return processAffinityMap;
            }

            do {
                if (pe32.th32ProcessID == 0 || pe32.th32ProcessID == 4 ||
                    pe32.th32ProcessID == GetCurrentProcessId()) {
                    continue;
                }

                DWORD_PTR affinity = GetProcessAffinityByPID(pe32.th32ProcessID);
                if (affinity != 0) {
                    processAffinityMap[pe32.th32ProcessID] = affinity;
                }
            } while (Process32Next(hSnapshot, &pe32));

            CloseHandle(hSnapshot);
            return processAffinityMap;
        }

        std::string CpuCoreManager::GetProcessName(DWORD processId) {
            HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
            if (hSnapshot == INVALID_HANDLE_VALUE) {
                return "Unknown";
            }

            PROCESSENTRY32 pe32;
            pe32.dwSize = sizeof(PROCESSENTRY32);

            if (Process32First(hSnapshot, &pe32)) {
                do {
                    if (pe32.th32ProcessID == processId) {
                        CloseHandle(hSnapshot);
                        return Utils::WideStringToString(pe32.szExeFile);
                    }
                } while (Process32Next(hSnapshot, &pe32));
            }

            CloseHandle(hSnapshot);
            return "Unknown";
        }

        bool CpuCoreManager::ExcludeCoreFromProcess(DWORD processId, DWORD coreToExclude) {
            HANDLE hProcess = OpenProcess(PROCESS_SET_INFORMATION | PROCESS_QUERY_INFORMATION, FALSE, processId);
            if (hProcess == NULL) {
                return false;
            }

            DWORD_PTR processAffinityMask = 0;
            DWORD_PTR systemAffinityMask = 0;

            if (!GetProcessAffinityMask(hProcess, &processAffinityMask, &systemAffinityMask)) {
                CloseHandle(hProcess);
                return false;
            }

            DWORD_PTR newAffinityMask = processAffinityMask & ~(1ULL << coreToExclude);

            if (newAffinityMask == 0) {
                for (int i = 0; i < 64; i++) {
                    if (systemAffinityMask & (1ULL << i) && i != coreToExclude) {
                        newAffinityMask = 1ULL << i;
                        break;
                    }
                }
            }

            bool success = SetProcessAffinityMask(hProcess, newAffinityMask);
            CloseHandle(hProcess);

            return success;
        }

        bool CpuCoreManager::ReserveCoreForCurrentProcess(DWORD reservedCore) {
            SYSTEM_INFO sysInfo = GetSystemInfo();

            if (reservedCore >= sysInfo.dwNumberOfProcessors) {
                std::cerr << "错误: 核心索引 " << reservedCore << " 超出范围" << std::endl;
                return false;
            }

            std::cout << "\n=== 为当前进程保留CPU核心 " << reservedCore << " ===" << std::endl;

            HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
            if (hSnapshot == INVALID_HANDLE_VALUE) {
                std::cerr << "创建进程快照失败" << std::endl;
                return false;
            }

            PROCESSENTRY32 pe32;
            pe32.dwSize = sizeof(PROCESSENTRY32);

            int processedCount = 0;
            int successCount = 0;
            int skipCount = 0;

            if (!Process32First(hSnapshot, &pe32)) {
                CloseHandle(hSnapshot);
                return false;
            }

            std::cout << "正在修改其他进程的CPU亲和性..." << std::endl;
            std::cout << "进程ID\t\t进程名\t\t\t\t状态" << std::endl;
            std::cout << "-------\t\t--------\t\t\t--------" << std::endl;

            do {
                if (pe32.th32ProcessID == 0 ||
                    pe32.th32ProcessID == 4 ||
                    pe32.th32ProcessID == GetCurrentProcessId() ||
                    IsSystemCriticalProcess(pe32.szExeFile)) {

                    std::string processName = Utils::WideStringToString(pe32.szExeFile);
                    std::cout << pe32.th32ProcessID << "\t\t" << processName << "\t\t\t跳过(系统进程)" << std::endl;
                    skipCount++;
                    continue;
                }

                processedCount++;

                DWORD_PTR currentAffinity = GetProcessAffinityByPID(pe32.th32ProcessID);
                if (currentAffinity & (1ULL << reservedCore)) {
                    std::string processName = Utils::WideStringToString(pe32.szExeFile);

                    if (ExcludeCoreFromProcess(pe32.th32ProcessID, reservedCore)) {
                        std::cout << pe32.th32ProcessID << "\t\t" << processName << "\t\t\t成功排除" << std::endl;
                        successCount++;

                        if (m_processDetectedCallback) {
                            m_processDetectedCallback(pe32.th32ProcessID, processName);
                        }
                    }
                    else {
                        std::cout << pe32.th32ProcessID << "\t\t" << processName << "\t\t\t失败(权限不足)" << std::endl;
                    }
                }
                else {
                    std::string processName = Utils::WideStringToString(pe32.szExeFile);
                    std::cout << pe32.th32ProcessID << "\t\t" << processName << "\t\t\t无需修改" << std::endl;
                }

            } while (Process32Next(hSnapshot, &pe32));

            CloseHandle(hSnapshot);

            std::cout << "\n=== 处理结果统计 ===" << std::endl;
            std::cout << "总进程数: " << (processedCount + skipCount) << std::endl;
            std::cout << "跳过系统进程: " << skipCount << std::endl;
            std::cout << "处理的进程: " << processedCount << std::endl;
            std::cout << "成功修改: " << successCount << std::endl;

            return true;
        }

        DWORD_PTR CpuCoreManager::AnalyzeOtherProcessesCPUUsage() {
            DWORD_PTR occupiedCores = 0;
            auto processAffinityMap = GetAllProcessesAffinity();

            std::cout << "\n=== 系统进程CPU亲和性分析 ===" << std::endl;
            std::cout << "进程ID\t\t亲和性掩码\t\t占用核心" << std::endl;
            std::cout << "-------\t\t----------\t\t--------" << std::endl;

            for (const auto& pair : processAffinityMap) {
                DWORD processId = pair.first;
                DWORD_PTR affinity = pair.second;

                occupiedCores |= affinity;

                std::cout << processId << "\t\t0x" << std::hex << affinity << std::dec << "\t\t\t";

                for (int i = 0; i < 64; i++) {
                    if (affinity & (1ULL << i)) {
                        std::cout << i << " ";
                    }
                }
                std::cout << std::endl;
            }

            return occupiedCores;
        }

        DWORD_PTR CpuCoreManager::GetAvailableCores() {
            SYSTEM_INFO sysInfo = GetSystemInfo();
            DWORD_PTR occupiedCores = AnalyzeOtherProcessesCPUUsage();
            DWORD_PTR availableCores = sysInfo.dwActiveProcessorMask & ~occupiedCores;

            std::cout << "\n=== CPU核心分配分析 ===" << std::endl;
            std::cout << "系统总核心掩码: 0x" << std::hex << sysInfo.dwActiveProcessorMask << std::dec << std::endl;
            std::cout << "其他进程占用: 0x" << std::hex << occupiedCores << std::dec << std::endl;
            std::cout << "可用核心掩码: 0x" << std::hex << availableCores << std::dec << std::endl;

            std::cout << "可用核心编号: ";
            for (int i = 0; i < sysInfo.dwNumberOfProcessors; i++) {
                if (availableCores & (1ULL << i)) {
                    std::cout << i << " ";
                }
            }
            std::cout << std::endl;

            return availableCores;
        }

        bool CpuCoreManager::SetIntelligentCPUAffinity() {
            DWORD_PTR availableCores = GetAvailableCores();

            if (availableCores == 0) {
                std::cout << "警告: 没有完全空闲的CPU核心，将使用最后一个核心" << std::endl;
                SYSTEM_INFO sysInfo = GetSystemInfo();
                availableCores = 1ULL << (sysInfo.dwNumberOfProcessors - 1);
            }

            std::cout << "\n设置当前进程使用CPU核心: 0x" << std::hex << availableCores << std::dec << std::endl;

            if (SetProcessAffinity(availableCores)) {
                std::cout << "成功设置当前进程使用剩余CPU核心" << std::endl;
                return true;
            }
            else {
                std::cerr << "设置CPU亲和性失败" << std::endl;
                return false;
            }
        }

        void CpuCoreManager::StartCoreProtection(DWORD reservedCore) {
            if (m_isProtectionActive) {
                StopCoreProtection();
            }

            m_protectedCore = reservedCore;
            m_protectedCores.clear();
            m_protectedCores.push_back(reservedCore);
            m_isProtectionActive = true;

            m_protectionThread = CreateThread(
                nullptr,
                0,
                ProtectionThreadProc,
                this,
                0,
                nullptr
            );

            if (m_protectionThread == nullptr) {
                std::cerr << "创建保护线程失败" << std::endl;
                m_isProtectionActive = false;
            } else {
                std::cout << "CPU核心保护线程已启动，保护核心: " << reservedCore << std::endl;
            }
        }

        void CpuCoreManager::StartMultiCoreProtection(const std::vector<DWORD>& reservedCores) {
            if (m_isProtectionActive) {
                StopCoreProtection();
            }

            if (reservedCores.empty()) {
                std::cerr << "没有指定要保护的核心" << std::endl;
                return;
            }

            m_protectedCores = reservedCores;
            m_protectedCore = reservedCores[0]; // 主保护核心
            m_isProtectionActive = true;

            m_protectionThread = CreateThread(
                nullptr,
                0,
                ProtectionThreadProc,
                this,
                0,
                nullptr
            );

            if (m_protectionThread == nullptr) {
                std::cerr << "创建多核心保护线程失败" << std::endl;
                m_isProtectionActive = false;
            } else {
                std::cout << "多核心保护线程已启动，保护核心: ";
                for (DWORD core : reservedCores) {
                    std::cout << core << " ";
                }
                std::cout << std::endl;
            }
        }

        void CpuCoreManager::StopCoreProtection() {
            if (m_isProtectionActive) {
                m_isProtectionActive = false;

                if (m_protectionThread != nullptr) {
                    WaitForSingleObject(m_protectionThread, 5000); // 等待5秒
                    CloseHandle(m_protectionThread);
                    m_protectionThread = nullptr;
                }

                std::cout << "CPU核心保护线程已停止" << std::endl;
            }
        }

        void CpuCoreManager::ProtectReservedCore(DWORD reservedCore, int durationSeconds) {
            std::cout << "\n开始保护CPU核心 " << reservedCore << " (" << durationSeconds << "秒)..." << std::endl;

            for (int i = 0; i < durationSeconds && m_isProtectionActive; i++) {
                HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
                if (hSnapshot != INVALID_HANDLE_VALUE) {
                    PROCESSENTRY32 pe32;
                    pe32.dwSize = sizeof(PROCESSENTRY32);

                    if (Process32First(hSnapshot, &pe32)) {
                        do {
                            if (pe32.th32ProcessID != GetCurrentProcessId() &&
                                pe32.th32ProcessID != 0 &&
                                pe32.th32ProcessID != 4) {

                                DWORD_PTR affinity = GetProcessAffinityByPID(pe32.th32ProcessID);
                                if (affinity & (1ULL << reservedCore)) {
                                    ExcludeCoreFromProcess(pe32.th32ProcessID, reservedCore);
                                    std::string processName = Utils::WideStringToString(pe32.szExeFile);
                                    std::cout << "检测到进程 " << pe32.th32ProcessID
                                        << " (" << processName << ") 使用保留核心，已自动排除" << std::endl;

                                    if (m_processDetectedCallback) {
                                        m_processDetectedCallback(pe32.th32ProcessID, processName);
                                    }
                                }
                            }
                        } while (Process32Next(hSnapshot, &pe32));
                    }
                    CloseHandle(hSnapshot);
                }

                std::this_thread::sleep_for(std::chrono::seconds(1));
            }
        }

        void CpuCoreManager::ProtectMultipleReservedCores(const std::vector<DWORD>& reservedCores, int durationSeconds) {
            std::cout << "\n开始保护多个CPU核心 (";
            for (size_t i = 0; i < reservedCores.size(); i++) {
                std::cout << reservedCores[i];
                if (i < reservedCores.size() - 1) std::cout << ", ";
            }
            std::cout << ") (" << durationSeconds << "秒)..." << std::endl;
            
            for (int i = 0; i < durationSeconds && m_isProtectionActive; i++) {
                HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
                if (hSnapshot != INVALID_HANDLE_VALUE) {
                    PROCESSENTRY32 pe32;
                    pe32.dwSize = sizeof(PROCESSENTRY32);
                    
                    if (Process32First(hSnapshot, &pe32)) {
                        do {
                            if (pe32.th32ProcessID != GetCurrentProcessId() && 
                                pe32.th32ProcessID != 0 && 
                                pe32.th32ProcessID != 4) {
                                
                                DWORD_PTR affinity = GetProcessAffinityByPID(pe32.th32ProcessID);
                                
                                // 检查是否占用了任何保留的核心
                                bool usingReservedCore = false;
                                DWORD conflictCore = 0;
                                
                                for (DWORD reservedCore : reservedCores) {
                                    if (affinity & (1ULL << reservedCore)) {
                                        usingReservedCore = true;
                                        conflictCore = reservedCore;
                                        break;
                                    }
                                }
                                
                                if (usingReservedCore) {
                                    ExcludeCoreFromProcess(pe32.th32ProcessID, conflictCore);
                                    std::string processName = Utils::WideStringToString(pe32.szExeFile);
                                    std::cout << "检测到进程 " << pe32.th32ProcessID 
                                             << " (" << processName << ") 使用保留核心 " << conflictCore << "，已自动排除" << std::endl;
                                    
                                    if (m_processDetectedCallback) {
                                        m_processDetectedCallback(pe32.th32ProcessID, processName);
                                    }
                                }
                            }
                        } while (Process32Next(hSnapshot, &pe32));
                    }
                    CloseHandle(hSnapshot);
                }
                
                std::this_thread::sleep_for(std::chrono::seconds(1));
            }
        }

        std::vector<DWORD> CpuCoreManager::GetRecommendedCores(DWORD coreCount, DWORD desiredCores) {
            std::vector<DWORD> recommendedCores;
            
            // 推荐策略：从最后的核心开始分配（通常系统负载较低）
            for (DWORD i = 0; i < desiredCores && i < coreCount; i++) {
                recommendedCores.push_back(coreCount - 1 - i);
            }
            
            return recommendedCores;
        }

        void CpuCoreManager::DisplayCoreAllocationStrategy(DWORD totalCores, DWORD desiredCores) {
            std::cout << "\n=== 核心分配策略 ===" << std::endl;
            std::cout << "系统总核心数: " << totalCores << std::endl;
            std::cout << "请求核心数: " << desiredCores << std::endl;
            
            auto recommendedCores = GetRecommendedCores(totalCores, desiredCores);
            
            std::cout << "推荐分配核心: ";
            for (DWORD core : recommendedCores) {
                std::cout << core << " ";
            }
            std::cout << std::endl;
            
            std::cout << "分配原则:" << std::endl;
            std::cout << "- 优先使用高编号核心（系统负载通常较低）" << std::endl;
            std::cout << "- 为系统保留低编号核心（0, 1 等）" << std::endl;
            std::cout << "- 确保系统关键进程有足够资源" << std::endl;
        }

        void CpuCoreManager::MonitorCPUUsage(int durationSeconds) {
            std::cout << "\n开始监控CPU使用情况（" << durationSeconds << "秒）..." << std::endl;
            
            for (int i = 0; i < durationSeconds; i++) {
                std::cout << "\n--- 第" << (i+1) << "秒监控结果 ---" << std::endl;
                GetAvailableCores();
                std::this_thread::sleep_for(std::chrono::seconds(1));
            }
        }

        void CpuCoreManager::SetProcessDetectedCallback(ProcessDetectedCallback callback) {
            m_processDetectedCallback = callback;
        }

        // 私有方法实现
        DWORD WINAPI CpuCoreManager::ProtectionThreadProc(LPVOID lpParam) {
            CpuCoreManager* pThis = static_cast<CpuCoreManager*>(lpParam);
            pThis->ProtectionThreadFunction();
            return 0;
        }

        void CpuCoreManager::ProtectionThreadFunction() {
            while (m_isProtectionActive) {
                HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
                if (hSnapshot != INVALID_HANDLE_VALUE) {
                    PROCESSENTRY32 pe32;
                    pe32.dwSize = sizeof(PROCESSENTRY32);
                    
                    if (Process32First(hSnapshot, &pe32)) {
                        do {
                            if (pe32.th32ProcessID != GetCurrentProcessId() && 
                                pe32.th32ProcessID != 0 && 
                                pe32.th32ProcessID != 4 &&
                                !IsSystemCriticalProcess(pe32.szExeFile)) {
                                
                                DWORD_PTR affinity = GetProcessAffinityByPID(pe32.th32ProcessID);
                                
                                // 检查是否使用了任何保护的核心
                                bool needExclusion = false;
                                DWORD conflictCore = 0;
                                
                                for (DWORD protectedCore : m_protectedCores) {
                                    if (affinity & (1ULL << protectedCore)) {
                                        needExclusion = true;
                                        conflictCore = protectedCore;
                                        break;
                                    }
                                }
                                
                                if (needExclusion) {
                                    ExcludeCoreFromProcess(pe32.th32ProcessID, conflictCore);
                                    
                                    if (m_processDetectedCallback) {
                                        std::string processName = Utils::WideStringToString(pe32.szExeFile);
                                        m_processDetectedCallback(pe32.th32ProcessID, processName);
                                    }
                                }
                            }
                        } while (Process32Next(hSnapshot, &pe32));
                    }
                    CloseHandle(hSnapshot);
                }
                
                Sleep(5000); // 每5秒检查一次
            }
        }

        bool CpuCoreManager::IsSystemCriticalProcess(const wchar_t* processName) {
            const wchar_t* criticalProcesses[] = {
                L"System", L"Registry", L"csrss.exe", L"winlogon.exe",
                L"services.exe", L"lsass.exe", L"wininit.exe", L"smss.exe"
            };

            for (const auto& critical : criticalProcesses) {
                if (wcscmp(processName, critical) == 0) {
                    return true;
                }
            }
            return false;
        }

        SYSTEM_INFO CpuCoreManager::GetSystemInfo() {
            SYSTEM_INFO sysInfo;
            ::GetSystemInfo(&sysInfo);
            return sysInfo;
        }

        // =============================================================================
        // Utils 命名空间实现
        // =============================================================================

        namespace Utils {

            bool IsRunningAsAdmin() {
                BOOL fIsElevated = FALSE;
                HANDLE hToken = NULL;
                TOKEN_ELEVATION elevation;
                DWORD dwSize;

                if (!OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, &hToken)) {
                    return false;
                }

                if (!GetTokenInformation(hToken, TokenElevation, &elevation, sizeof(elevation), &dwSize)) {
                    CloseHandle(hToken);
                    return false;
                }

                fIsElevated = elevation.TokenIsElevated;
                CloseHandle(hToken);
                return fIsElevated != FALSE;
            }

            bool RestartAsAdmin() {
                wchar_t szPath[MAX_PATH];
                if (GetModuleFileNameW(NULL, szPath, ARRAYSIZE(szPath))) {
                    SHELLEXECUTEINFOW sei;
                    ZeroMemory(&sei, sizeof(sei));
                    sei.cbSize = sizeof(sei);
                    sei.lpVerb = L"runas";
                    sei.lpFile = szPath;
                    sei.hwnd = NULL;
                    sei.nShow = SW_NORMAL;

                    if (!ShellExecuteExW(&sei)) {
                        DWORD dwError = GetLastError();
                        if (dwError == ERROR_CANCELLED) {
                            std::wcerr << L"用户取消了管理员权限请求" << std::endl;
                        }
                        else {
                            std::wcerr << L"无法以管理员权限启动程序. 错误码: " << dwError << std::endl;
                        }
                        return false;
                    }
                    return true;
                }
                return false;
            }

            bool EnablePrivilege(const TCHAR* privilegeName, bool enable) {
                HANDLE hToken = nullptr;
                if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, &hToken)) {
                    return false;
                }

                LUID luid;
                if (!LookupPrivilegeValue(nullptr, privilegeName, &luid)) {
                    CloseHandle(hToken);
                    return false;
                }

                TOKEN_PRIVILEGES tokenPriv;
                tokenPriv.PrivilegeCount = 1;
                tokenPriv.Privileges[0].Luid = luid;
                tokenPriv.Privileges[0].Attributes = enable ? SE_PRIVILEGE_ENABLED : 0;

                if (!AdjustTokenPrivileges(hToken, FALSE, &tokenPriv, sizeof(tokenPriv), nullptr, nullptr)) {
                    CloseHandle(hToken);
                    return false;
                }

                CloseHandle(hToken);
                return true;
            }

            void DisplaySystemInfo() {
                SYSTEM_INFO sysInfo;
                GetSystemInfo(&sysInfo);

                std::cout << "=== 系统信息 ===" << std::endl;
                std::cout << "CPU核心数: " << sysInfo.dwNumberOfProcessors << std::endl;
                std::cout << "页面大小: " << sysInfo.dwPageSize << " bytes" << std::endl;
                std::cout << "处理器架构: ";

                switch (sysInfo.wProcessorArchitecture) {
                case PROCESSOR_ARCHITECTURE_AMD64:
                    std::cout << "x64 (AMD or Intel)" << std::endl;
                    break;
                case PROCESSOR_ARCHITECTURE_ARM:
                    std::cout << "ARM" << std::endl;
                    break;
                case PROCESSOR_ARCHITECTURE_ARM64:
                    std::cout << "ARM64" << std::endl;
                    break;
                case PROCESSOR_ARCHITECTURE_INTEL:
                    std::cout << "x86" << std::endl;
                    break;
                default:
                    std::cout << "Unknown" << std::endl;
                    break;
                }
            }

            DWORD GetCpuCoreCount() {
                SYSTEM_INFO sysInfo;
                GetSystemInfo(&sysInfo);
                return sysInfo.dwNumberOfProcessors;
            }

            DWORD_PTR GetSystemAffinityMask() {
                SYSTEM_INFO sysInfo;
                GetSystemInfo(&sysInfo);
                return sysInfo.dwActiveProcessorMask;
            }

            std::string WideStringToString(const wchar_t* wideStr) {
                if (wideStr == nullptr) return "";

                int sizeNeeded = WideCharToMultiByte(CP_UTF8, 0, wideStr, -1, nullptr, 0, nullptr, nullptr);
                if (sizeNeeded <= 0) return "";

                std::string result(sizeNeeded - 1, 0);
                WideCharToMultiByte(CP_UTF8, 0, wideStr, -1, &result[0], sizeNeeded, nullptr, nullptr);
                return result;
            }

            std::wstring StringToWideString(const std::string& str) {
                if (str.empty()) return L"";

                int sizeNeeded = MultiByteToWideChar(CP_UTF8, 0, str.c_str(), -1, nullptr, 0);
                if (sizeNeeded <= 0) return L"";

                std::wstring result(sizeNeeded - 1, 0);
                MultiByteToWideChar(CP_UTF8, 0, str.c_str(), -1, &result[0], sizeNeeded);
                return result;
            }
        }
    }
}