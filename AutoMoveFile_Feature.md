# 自动移动文件（AutoMoveFile）功能说明

## 概述
AutoMoveFile 是仓库中负责自动将文件从一个目录移动到另一个目录的服务组件。该服务周期性扫描配置中指定的源目录，并将匹配的文件按相对路径移动到目标目录，同时记录操作日志与移动记录文件。

## 主要功能点

- 支持配置多个独立移动任务（每个任务为一个 `[MoveTask]` 节）。
- 保留源目录的相对目录结构，将文件移动到目标目录的对应路径。
- 可配置时间限制（分钟），在时间限制范围内最近修改的文件将被跳过，不会被移动。
- 会为每个源目录生成当天的移动记录文件（`移动记录_yyyyMMdd.txt`），记录移动/跳过/失败等操作。
- 自动创建目标目录（如果不存在）。
- 处理目标文件已存在的情况，会为新文件生成唯一文件名（例如 `file(1).txt`）。
- 具有异常处理和日志记录，保证运行稳定且可追溯。

## 配置说明（AutoMoveFile.ini）
配置文件路径：`{应用程序根目录}\AutoMoveFile.ini`。

每个移动任务使用一个配置节示例：

[MoveTask]
SourceDirectory=D:\MoveSource
TargetDirectory=E:\MoveTarget
MoveTimeLimitMinutes=0

参数说明：
- `SourceDirectory`：源目录，必填。
- `TargetDirectory`：目标目录，必填。
- `MoveTimeLimitMinutes`：移动时间限制（整数，单位：分钟）。
  - 0 或负数表示不启用时间限制（默认）。
  - 如果一个文件的 `LastWriteTime` 与当前时间之差小于等于该值，则该文件被跳过，不会移动。

配置规则：
- 支持多个 `[MoveTask]` 节，服务会遍历每个任务并分别执行移动。
- 配置键名不区分大小写。

## 行为细节

1. 启动
   - 调用 `AutoMoveFile.Start()` 启动服务，内部通过 `Task.Run(Run)` 在后台循环执行。
   - 每次循环读取 `AutoMoveFile.ini` 并解析为多个 `AutoMoveConfig`。

2. 任务执行（ExecuteMoveTask）
   - 对每个有效配置，检查 `SourceDirectory` 与 `TargetDirectory` 是否存在并有效。
   - 如果目标目录不存在则自动创建。
   - 为当天生成移动记录文件：`移动记录_yyyyMMdd.txt`，并确保文件头存在。
   - 递归收集源目录下的所有文件（排除以 `移动记录` 开头的文件名）。
   - 遍历文件列表，对每个文件调用 `MoveFile` 执行移动。

3. 时间限制检查（IsFileWithinTimeLimit）
   - 若配置的 `MoveTimeLimitMinutes <= 0`，视为不启用时间限制。
   - 获取文件最后修改时间 `FileInfo.LastWriteTime`，计算与 `DateTime.Now` 的差值（分钟）。
   - 若差值 <= 配置值，则认为文件“在时间限制范围内”，将跳过移动。
   - 异常情况下（读取时间失败等），默认不限制该文件移动（返回 false）。

4. 移动逻辑（MoveFile）
   - 首先执行时间限制检查，若应当跳过：
     - 写入移动记录（操作为 `跳过`），记录原因（最后修改时间与距离现在的分钟数）。
     - 在日志中写入 `跳过移动文件` 信息。
   - 否则：
     - 计算源文件相对于 `SourceDirectory` 的相对路径，拼接到 `TargetDirectory` 保持目录结构。
     - 若目标路径所在目录不存在则创建。
     - 若目标文件已存在，则通过 `GenerateUniqueFileName` 生成新文件名以避免覆盖。
     - 使用 `File.Move` 执行移动。
     - 写入移动记录（操作为 `移动`，状态 `成功`），并在日志中写入 `移动文件`。
   - 对异常情况进行捕获：记录日志并在移动记录中写入失败信息。

## 日志与记录

- 使用 `LogHelper.Logger` 记录：启动/停止/异常/创建目录/移动/跳过等事件。
- 每个源目录会在其目录下生成 `移动记录_yyyyMMdd.txt`，记录时间、操作类型、源路径、目标路径和状态/原因。

移动记录示例行：
```
[2025-08-15 12:34:56] 移动 - D:\Source\a.txt => E:\Target\a.txt - 成功
[2025-08-15 12:40:00] 跳过 - D:\Source\b.txt => N/A - 文件在时间限制范围内（最后修改时间：2025-08-15 12:35:05，距离现在：4.9分钟）
```

## 接口/类说明（关键成员）

- `class AutoMoveConfig`
  - `string SourceDirectory`
  - `string TargetDirectory`
  - `int MoveTimeLimitMinutes`

- `class AutoMoveFile`
  - `static void Start()`：启动服务
  - `static void Stop()`：停止服务
  - `private static void Run()`：后台循环主逻辑
  - `private static void ReadIniFile()`：读取并解析配置文件
  - `private static void ExecuteMoveTask(AutoMoveConfig)`：执行单个任务
  - `private static List<string> GetFilesToMove(string)`：列出文件
  - `private static bool IsFileWithinTimeLimit(string, int)`：判断文件是否在时间限制内
  - `private static MoveResult MoveFile(..., AutoMoveConfig)`：移动文件并返回结果
  - `private static void WriteMoveRecord(...)`：写入移动记录文件

## 注意事项与建议

- 时间计算使用本地时间（DateTime.Now），若需要跨时区或更精确一致性，建议使用 UTC 时间（DateTime.UtcNow）并调整 `LastWriteTimeUtc`。
- `MoveTimeLimitMinutes` 默认 0，保持原有行为，不用修改现有配置即可兼容。
- 如果源目录中存在大量文件，建议合理设置扫描频率或在外部触发移动以避免性能问题。
- 若需要忽略某些文件类型或增加更多筛选条件，可以在 `GetFilesToMove` 中加入相应规则。

## 更改历史
- 2025-08-15：新增 `MoveTimeLimitMinutes` 配置；在移动前检查文件 `LastWriteTime`，若在限制内则跳过；记录跳过原因并在日志中输出。

---

以上为 `AutoMoveFile` 的功能说明文档，已写入仓库：`AutoMoveFile_Feature.md`。
