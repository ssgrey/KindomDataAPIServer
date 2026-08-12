# KindomDataAPIServer 同步性能与稳定性优化交接文档

更新时间：2026-08-12  
项目目录：`D:\DownLoad\KindomFile\CommercialSDK_2025_19.0.00090.0-shixin\CommercialSDK_2025_19.0.00090.0\SampleProjects\KindomDataAPIServer`

## 一、目标

本轮计划解决以下问题：

1. 将主程序由 Any CPU/32 位偏好改为明确的 x64，解除 32 位地址空间限制。
2. 将固定的 1.5 GiB 本地内存保护阈值改为可配置的 10 GiB 高水位和 8 GiB 低水位。
3. 内存压力只负责暂停生产、排空上传队列和每轮压力周期降低一次读取批次，不再反复降低 payload 或 HTTP 并发。
4. 准确识别服务端返回的 HTTP 413 `RequestEntityTooLarge`。
5. HTTP 413 不再被当作网络错误，也不触发通用 payload 减半和并发下降。
6. 过大的上传批次自动拆分并重试，避免失败批次直接丢失。
7. 并发请求同时返回 413 时，保护调整不能叠加。
8. 只持久化稳定、成功的自适应参数，不保存临时保护值。
9. 保留用户已经手动设置的 `maxPayloadMiB: 28`。

## 二、问题分析结论

### 1.5 GiB 限制的性质

原来的 1.5 GiB 是客户端 `AdaptiveSyncService` 中硬编码的进程私有内存保护阈值，不是服务端限制，也不是 Windows 给程序设置的硬性内存上限。

程序超过该阈值后，原保护逻辑会不断降低读取批次、payload 和上传并发，从而造成任务后半段吞吐快速下降。

### x64 与大内存

原 EXE 为 Any CPU，并带有 32 位偏好，实际可能以 32 位进程运行。32 位进程受虚拟地址空间限制，即使机器物理内存很大，也无法正常使用几十 GiB 内存。

改为明确的 x64 后，程序可以使用超过 4 GiB、甚至超过 32 GiB 的内存。实际可用量仍取决于：

- 机器物理内存和页面文件；
- Windows 和 .NET Framework 的虚拟地址空间；
- 对象布局、GC 和内存碎片；
- 应用自己的 10/8 GiB 背压保护；
- 上传队列和业务批次规模。

### HTTP 413 的来源

`RequestEntityTooLarge` 是服务端接口返回的 HTTP 413。已确认服务端默认请求体上限为 `30,000,000` bytes。

历史失败轨迹请求的 JSON 大小为 `30,938,645` bytes，超过服务端限制，因此被拒绝。原客户端将此异常归类为 `network-retry`，随后快速降低 payload 和并发，但失败批次本身没有拆分重试，造成数据缺失。

用户已将所有数据类型的 `maxPayloadMiB` 设置为 28。28 MiB 等于 `29,360,128` bytes，比服务端限制约少 625 KiB。这个余量合理，但仍必须实现 413 拆分重试，以应对序列化差异、服务端配置变化和单条超大数据。

## 三、已经修改的内容

以下改动当前已存在于工作区，并已完成 Debug/Release x64 构建验证。

### `KindomDataAPIServer/AdaptiveSyncSettings.json`

- 保留用户手动修改的五个 `maxPayloadMiB: 28`。
- 在 `common` 中增加：

```json
"memoryHighWatermarkMiB": 10240,
"memoryLowWatermarkMiB": 8192
```

### `KindomDataAPIServer/Common/AdaptiveSyncService.cs`

- 增加 `MemoryHighWatermarkMiB`，默认 10240 MiB。
- 增加 `MemoryLowWatermarkMiB`，默认 8192 MiB。
- 五种数据类型的 C# fallback `MaxPayloadMiB` 调整为 28。
- 删除固定的 `MemoryPressureBytes = 1536 MiB`。
- 根据配置计算高、低水位字节数。
- 使用滞回状态管理内存压力：达到 10 GiB 进入，降至 8 GiB 才退出。
- 每个内存压力周期只将读取批次降低一次。
- 内存压力不再调用通用 `ApplyProtection`，因此不持续降低 payload 或 HTTP 并发。
- 增加 `RecordRequestTooLarge`，413 走独立处理路径。
- 增加内存水位配置的范围校验。

`RecordUpload` 现在先识别 413，再决定是否设置 `_transportOrInternalFailure`。已通过拆分恢复的 413 不会永久标记学习失败；不可恢复的单条 413 由上传消费者显式记录内部失败。

### `KindomDataAPIServer/DataService/ApiRequestTelemetry.cs`

- 增加 `IsRequestTooLarge`：通过状态码 413 或 `request-too-large` signal 判断。
- 从 `HasProtectionSignal` 中排除 413，避免它触发通用保护。

### `KindomDataAPIServer/DataService/APIClient.cs`

- JSON POST 和 multipart POST 的非重试异常携带 HTTP 状态码。
- `NonRetryableHttpRequestException` 增加 `StatusCode` 属性和相应构造函数。
- 在通用 `HttpRequestException` 判断之前，将 413 分类为 `request-too-large`。

所有 `NonRetryableHttpRequestException` 构造调用均已传入 HTTP 状态码。发布 telemetry 的 JSON POST 和 multipart POST 可以稳定识别 413。

### `KindomDataAPIServer/KindomAPI/KingdomAPI.cs`

- `EnqueueAdaptive<T>` 在入队前检查内存压力。
- 内存压力期间暂停生产者入队并等待，同时更新队列占用信息。

formation、轨迹、well log、production 和 well test 均已实现 413 失败批次的消费者本地二分重试。

### x64 项目配置

已修改：

- `KindomDataAPIServer/KindomDataAPIServer.csproj`
- `Tet.GeoSymbol/Tet.GeoSymbol.csproj`
- `KindomDataAPIServer.sln`

已增加 Debug/Release x64 配置和项目映射，并设置：

```xml
<PlatformTarget>x64</PlatformTarget>
<Prefer32Bit>false</Prefer32Bit>
```

Release 下 `Tet.GeoSymbol` 映射到已有的 `TRelease|x64`。

## 四、后续必须完成的实现

### 1. 轨迹上传 413 拆分重试（已完成）

位置：`KindomDataAPIServer/KindomAPI/KingdomAPI.cs`，轨迹上传消费者工作线程附近。

建议实现：

1. 每个上传消费者为当前原始批次创建本地栈或队列。
2. 上传批次返回 413 时，将 `WellTrajRequest.Items` 二分。
3. 两个子批次放入消费者本地栈，不重新放回全局 `BlockingCollection`，避免所有消费者因有界队列满而死锁。
4. 每个子批次重新计算：
   - `ItemCount`；
   - `DataPointCount`，即各 item 的 `CoordList.Count` 总和；
   - JSON `PayloadBytes`；
   - 仅包含子批次井 ID 的 `WellUwisById`。
5. traceName 和英文诊断日志增加 split depth/part 标识。
6. 单个 item 仍返回 413 时，记录最终失败并停止继续拆分，避免无限循环。
7. 只有原始批次的全部子批次处理完成后，才报告该原始批次的进度。
8. 每次实际 HTTP 请求都计入 upload attempt 和 payload bytes。
9. `syncedTrajectoryCount` 只统计成功上传的子批次 item，不能重复计算。
10. 已经通过拆分恢复的 413 不应将整个任务标记为失败；最终不可恢复的单条 413 才记录任务失败。

### 2. 其他数据类型的 413 保护（已完成）

已按同样原则处理：

- formation：按 `PbWellFormationList.Datas` 二分，并按子批次 `WellId` 同步筛选 `FileRows` 和 UWI 映射；兼容 protobuf 和文件导入模式。
- well log：按 `PbWellLogCreateList.LogList` 二分，并重新计算曲线数、样点数、protobuf 字节和 UWI 映射。
- production：按 `WellProductionDataRequest.Items` 二分，并重新计算井数、日产数据数、JSON 字节和 UWI 映射。
- well test：按 gas/oil request 的 `Items` 二分，并重新计算试井记录数、JSON 字节和 UWI 映射。

所有类型都使用消费者本地栈，不把子批次放回全局有界队列。单个最小可拆分 item 仍返回 413 时记录最终失败，不再递归。每个实际 HTTP 尝试（包括通用重试）均计入 upload attempt 和 payload bytes。

### 3. 413 防叠加（已完成）

`RecordRequestTooLarge` 使用幂等周期标志：同一保护周期内的并发 413 只计一次保护，不降低并发、不连续减半 payload；达到配置的稳定窗口数后清除周期标志。

基本原则：

- 413 不降低 HTTP 并发。
- 413 不连续减半 payload。
- 当前失败批次通过拆分解决。
- 新批次继续遵守 28 MiB 上限。
- 稳定成功若干窗口后才重置 413 周期标志。

### 4. 学习状态保护（已完成）

当前已保证：

- 只有任务最终成功、至少两个有效稳定窗口且最后不是保护调整时，才写入学习状态。
- 内存压力导致的临时 read batch 降低不能作为新的最佳稳定 read batch。
- 413 或其他保护窗口中的 payload 不能成为新的最佳稳定 payload。
- 没有产生新的稳定成功采样时，保留原来的学习状态，不覆盖为临时值。
- 拆分后恢复的 413 可以继续运行，但包含 413 的窗口不能参与稳定学习。

## 五、构建与验证状态

已使用以下 MSBuild 完成构建：

```powershell
& "D:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" KindomDataAPIServer.sln /t:Build /p:Configuration=Debug /p:Platform=x64 /m
& "D:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" KindomDataAPIServer.sln /t:Build /p:Configuration=Release /p:Platform=x64 /m
```

结果：全部五类 413 拆分完成后，Debug x64 和 Release x64 均重新构建成功。构建仍有仓库原有的重复 `using`、未使用变量和未赋值字段警告，没有新增编译错误。

使用 `CorFlags.exe` 检查 Debug/Release 输出，二者均为 `PE32+`，`32BITREQ:0`，`32BITPREF:0`。

验证项：

1. Debug 和 Release x64 均可构建。
2. 输出 EXE 为 x64/PE32+，且不存在 32 位偏好。
3. 执行 `git diff --check`。
4. 确认五处 `maxPayloadMiB` 仍为 28。
5. 检查同步任务报告包含英文的读取参数、返回数量、payload 字节、请求结果和耗时。
6. 检查 413 拆分日志、子批次统计和最终任务状态。
7. 未经用户明确要求，不得回放真实采集的上传请求或向后端写入数据。

本轮未执行第 6 项真实后端回放，因为该验证会写入后端且用户没有明确授权；代码路径已通过静态审阅和双配置编译验证。

## 六、当前工作区文件

当前已有修改：

```text
KindomDataAPIServer.sln
KindomDataAPIServer/AdaptiveSyncSettings.json
KindomDataAPIServer/Common/AdaptiveSyncService.cs
KindomDataAPIServer/DataService/APIClient.cs
KindomDataAPIServer/DataService/ApiRequestTelemetry.cs
KindomDataAPIServer/KindomAPI/KingdomAPI.cs
KindomDataAPIServer/KindomDataAPIServer.csproj
KindomDataAPIServer/ViewModels/SyncKindomDataViewModel.cs
Tet.GeoSymbol/Tet.GeoSymbol.csproj
```

注意：开始修改前，用户已有的唯一工作区变更是 `AdaptiveSyncSettings.json` 中五处 `maxPayloadMiB: 28`。不得撤销这些修改，也不要覆盖其他用户变更。

## 七、新对话建议指令

新对话可以直接发送：

```text
请读取仓库根目录的 AdaptiveSyncOptimizationPlan.md 和 AGENTS.md，复核当前工作区改动。五类上传的 HTTP 413 本地拆分重试、自适应学习状态保护、并发 413 防叠加和 Debug/Release x64 构建验证已完成。保留 AdaptiveSyncSettings.json 中五处 maxPayloadMiB: 28。下一步如获明确授权，可在隔离测试后端验证 413 拆分日志、子批次统计、进度和最终任务状态；未经授权不要回放真实上传请求或写入后端数据。
```

## 八、范围说明

当前代码已完成文档中的实现范围并通过编译验证。尚未完成的是需要写入后端的真实 413 回放验证；新对话必须先检查 `git diff`，基于现有修改继续，不能从头覆盖或回滚。
