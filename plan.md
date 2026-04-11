User: 我现在需要修改这个系统，允许Agent执行stathub服务器下发的命令并汇报结果。

你先理解一下它们的通信过程，尝试讨论如何支持

Gemini: Research agent-server communication

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.Client/Services)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.SDK/Models)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.SDK/AddressModels)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub/Services)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub/Data)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.Client/Services/Stat)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub/Services/Authentication)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub/Services/BackgroundJobs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub/Services/FileStorage)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.Client/Program.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.Client/Startup.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.Client/ClientHandler.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.SDK/ServerAccess.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.SDK/ServerConfig.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.SDK/Extensions.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.Client/Services/ServerMonitor.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.Client/Services/SubmitService.cs#1-1), lines 1 to 80

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.Client/Services/ClientIdService.cs#1-1), lines 1 to 50

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub/Controllers/ApiController.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub/Controllers/DashboardController.cs#1-1), lines 1 to 100

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub/Data/InMemoryDatabase.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.SDK/Models/Agent.cs#1-1), lines 1 to 120

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.SDK/Models/AgentStatus.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.SDK/Models/AgentHealthReport.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.SDK/Models/DstatResult.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.SDK/Models/ContainerInfo.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.SDK/Models/DiskSpaceInfo.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.SDK/AddressModels/MetricsAddressModel.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.Client/Services/SubmitService.cs#80-80), lines 80 to 80

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.Client/Services/ClientIdService.cs#50-50), lines 50 to 64

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.SDK/Models/Agent.cs#120-120), lines 120 to 302

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.Client/Services/SubmitService.cs#80-80), lines 80 to 101

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.Client/Services/Stat/DockerService.cs#1-1), lines 1 to 80

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.Client/Services/Stat/SkuInfoService.cs#1-1), lines 1 to 60

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub/Services/BackgroundJobs/DummyJob.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub/Services/IpGeolocationService.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/tests/UnitTests/AgentTests.cs#1-1), lines 1 to 80

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/tests/UnitTests/DstatResultTests.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/tests/IntegrationTests/ContainerStatusTests.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub/Controllers)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub/Controllers/DashboardController.cs#100-100), lines 100 to 100

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub/Services/Extensions.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub/Services/BackgroundJobs/OrphanAvatarCleanupJob.cs#1-1), lines 1 to 80

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub/Controllers/MetricsController.cs#1-1), lines 1 to 100

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub/Startup.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.SDK/Models/CpuInfo.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub/Controllers/MetricsController.cs#100-100), lines 100 to 146

研究完毕。现在我来理解整体架构，然后问几个关键问题。

---

**当前通信架构（只有单向）：**

```
Agent (Client)  →  POST /api/metrics (每9-10秒)  →  Server
Browser         ←  WebSocket 流推送实时指标        ←  Server
```

Agent 周期性 push 数据，服务器没有任何下发指令的机制。

现有的三种可扩展方式：

- **A. 响应体夹带**：在 `POST /api/metrics` 的响应 JSON 里附带 `commands[]`，Agent 读取并执行。最简单，但延迟高（最多10秒）
- **B. 轮询端点**：Agent 提交 metrics 后再 `GET /api/commands/{clientId}`，服务器返回待执行命令队列
- **C. WebSocket 长连接**：Agent 建立一条持久 WebSocket 到服务器，服务器随时下发命令（类似现有的 `MetricsController` 对浏览器的推送，方向反过来） 

Asked 4 questions (命令类型, 通信方式, 结果展示, 权限控制)

Asked a question (命令类型)

好的，现在我有足够信息来设计方案了。

---

## Plan: Agent WebSocket 命令下发与结果回传

**设计思路：** 服务器新增一个 WebSocket 端点供 Agent 长连接，Dashboard 用户通过现有权限向 Agent 下发任意 shell 命令，Agent 执行后通过 `POST /api/command-result` 上报，服务器以内存队列存储历史记录并在 Dashboard 详情页展示。命令管道利用 `Agent` 模型上的 `AsyncObservable<Command>` 实现，与现有 `Stats` 流模式对称。

---

**Steps**

**1. SDK：新增数据模型**

- 在 Models 新增 `Command.cs`（字段：`Id` Guid、`Text` string、`IssuedAt` DateTime）
- 新增 `CommandExecution.cs`（字段：Command + `ExitCode` int、`Stdout/Stderr` string、`ExecutedAt` DateTime、`AgentClientId` string）
- 在 AddressModels 新增 `CommandResultAddressModel.cs`（Agent 上报用：`ClientId`、`CommandId`、`ExitCode`、`Stdout`、`Stderr`）

**2. SDK：扩展 `ServerAccess`**

在 ServerAccess.cs 新增两个方法：
- `ConnectCommandChannelAsync(clientId)` → 建立 WebSocket 连接到 `GET /api/agent/{clientId}/commands.ws`，返回 `WebSocket`
- `SubmitCommandResultAsync(model)` → `POST /api/command-result`

**3. Server：`Agent` 模型增加命令通道**

在 Agent.cs 新增：
- `AsyncObservable<Command> PendingCommands` — 与现有 `Stats` 字段平行，供 WebSocket 端点订阅推送命令
- `ConcurrentQueue<CommandExecution> CommandHistory`（最多保留最近 50 条）

**4. Server：新增 `AgentCommandController`**

新建 src/Aiursoft.StatHub/Controllers/AgentCommandController.cs（`[AllowAnonymous]`，通过 `clientId` 识别 Agent）：
- `GET /api/agent/{clientId}/commands.ws` — 接受 WebSocket 升级，订阅 `agent.PendingCommands`，将每条 `Command` 序列化为 JSON 推送给 Agent，阻塞直到断连

**5. Server：`ApiController` 新增结果接收端点**

在 ApiController.cs 新增：
- `POST /api/command-result` — 反序列化 `CommandResultAddressModel`，找到对应 Agent，将结果追加到 `Agent.CommandHistory`

**6. Server：`DashboardController` 新增下发端点**

在 DashboardController.cs 新增：
- `POST /Dashboard/SendCommand/{clientId}` —`[Authorize(CanViewDashboard)]`，将新 `Command` 推入对应 `Agent.PendingCommands` 通道
- `GET /Dashboard/Commands/{clientId}` — 返回 `Agent.CommandHistory` 历史页面（或直接集成到 Details 页）

**7. Server：Dashboard 详情页 UI**

在 Details.cshtml 增加：
- "执行命令"表单（`<textarea>` + 提交按钮，POST 到 `/Dashboard/SendCommand/{id}`）
- 命令历史列表（展示命令文本、ExitCode、stdout/stderr 折叠、执行时间）

**8. Client：新增 `CommandListenerService`**

在 Services 新建 `CommandListenerService.cs`：
- 在 `StartAsync` 中调用 `ServerAccess.ConnectCommandChannelAsync(clientId)` 建立 WebSocket
- 循环读取 JSON 消息，解析为 `Command`
- 用 `Process.Start("bash", $"-c \"{command.Text}\"")` 执行，超时 60s
- 采集 stdout/stderr 和 ExitCode，调用 `ServerAccess.SubmitCommandResultAsync(...)` 上报
- 断连后自动重试（指数退避）

**9. Client：注册 `CommandListenerService`**

在 Startup.cs 将 `CommandListenerService` 注册为 `IHostedService`（与现有 `ServerMonitor` 并列）

---

**Verification**

- 单元测试：`CommandListenerService` 的进程执行与结果解析（仿照现有 AgentTests.cs）
- 集成测试：仿照 ContainerStatusTests.cs，验证命令通道 WebSocket 端点、结果 POST 端点
- 手动验证：启动 Server + Agent，在 Dashboard 输入 `echo hello`，验证 ExitCode=0、stdout="hello" 出现在历史

---

**Decisions**

- WebSocket 而非轮询：实时性更好，与现有 `MetricsController` 推流模式对称，Server 侧无需主动维护命令队列 TTL
- `[AllowAnonymous]` + clientId 认证：与现有 `/api/metrics` 一致，不引入新的 Auth 机制
- 命令类型为任意 shell：跳过了类型限制问题，后续可在此基础上加白名单配置项
- 历史仅保留最近 50 条内存存储：与整个 Agent 状态保持一致（无持久化 DB），避免引入 ORM 迁移

---

这个方案有没有你想调整的地方？比如命令有无超时限制要求、是否需要支持 Windows Agent、或者结果是否要实时流式回传（而不是执行完再 POST）？

User: 那这样仍然涉及到了HTTP传输。我们能否让每个客户端hold住一个websocket，然后全走websocket传输？

Gemini: 好问题。把 metrics 上报也迁移进同一条 WebSocket，让整个 Agent ↔ Server 通信完全走单一长连接，这比混用 HTTP + WebSocket 更干净。我来研究一下现有代码的细节看改动范围。 

Deep dive key files

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.Client/ClientHandler.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.Client/Startup.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.Client/Services)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.SDK/ServerAccess.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.SDK/ServerConfig.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub/Controllers/ApiController.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.SDK/AddressModels)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.Client/Services/ServerMonitor.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.Client/Services/SubmitService.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub/Controllers/MetricsController.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub/Data/InMemoryDatabase.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.SDK/Models/Agent.cs#1-1), lines 1 to 300

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.SDK/AddressModels/MetricsAddressModel.cs)

Read [](file:///home/anduin/Source/Repos/Aiursoft/aspnetcore-web/StatHub/src/Aiursoft.StatHub.SDK/Models/Agent.cs#300-300), lines 300 to 302

好，我现在有了完整的代码细节，可以设计一个纯 WebSocket 方案了。

---

## Plan: 全 WebSocket 双向通信通道

**设计思路：** 用单条持久 WebSocket 连接（`ws://{server}/api/agent/{clientId}/channel`）完全替代当前的 `POST /api/metrics` HTTP 轮询。连接上行传指标和命令结果，下行传命令，消息格式统一用 JSON 信封 `{ "type": "...", "payload": {...} }`。服务器侧 Agent WebSocket 端点使用原生 `System.Net.WebSockets` 双向模式（不同于现有 `MetricsController` 里专门用于单向推浏览器的 pusher 抽象）。

---

**通信协议**

```
Agent → Server  type: "metrics"          payload: MetricsAddressModel
Agent → Server  type: "command-result"   payload: { commandId, exitCode, stdout, stderr }
Server → Agent  type: "command"          payload: { id, text }
```

---

**Steps**

**1. SDK：新增消息模型**

在 Models 新增：
- `Command.cs` — `Id` (Guid)、`Text` (string)、`IssuedAt` (DateTime)
- `CommandExecution.cs` — 含 `Command`、`ExitCode` (int)、`Stdout/Stderr` (string)、`ExecutedAt` (DateTime)
- `AgentWsMessage.cs` — 客户端发出的信封：`Type` (string) + `Payload` (JToken / object)
- `ServerWsMessage.cs` — 服务器发出的信封：同结构

**2. SDK：`Agent` 模型增加命令通道**

在 Agent.cs 新增：
- `AsyncObservable<Command> PendingCommands` — Dashboard 触发 → 服务器消费并写入 WebSocket 下发给 Agent
- `ConcurrentQueue<CommandExecution> CommandHistory` — Agent 上报后追加，最多保留 100 条

**3. Server：新增 `AgentChannelController`**

新建 src/Aiursoft.StatHub/Controllers/AgentChannelController.cs：
- `GET /api/agent/channel` (`[AllowAnonymous]`，`[EnforceWebSocket]`)，通过 query string 接收 `clientId`
- 使用 `HttpContext.WebSockets.AcceptWebSocketAsync()` 获取原生双向 `WebSocket`
- 启动两个并发 Task：
  - **读循环**：循环 `ReceiveAsync` → 反序列化 JSON 信封 → `type == "metrics"` 则更新 Agent 状态和广播 Stats（复用 `ApiController.Metrics` 的同款逻辑）；`type == "command-result"` 则追加到 `Agent.CommandHistory`
  - **写循环**：订阅 `agent.PendingCommands`，逐条序列化为 `ServerWsMessage` 并 `SendAsync`
- 断连时两个 Task 都优雅取消，并将 `Agent.LastUpdate` 停止刷新使其变为 Offline 状态

**4. Server：`ApiController` 去掉 `/api/metrics` 端点**

ApiController.cs 只保留 `GET /api/info`（保留一个 HTTP 健康检查），删除 `POST /api/metrics`。地理位置解析逻辑随对应逻辑移入 `AgentChannelController` 的读循环。

**5. Server：`DashboardController` 新增下发端点**

在 DashboardController.cs 新增：
- `POST /Dashboard/SendCommand/{clientId}` — `[Authorize(CanViewDashboard)]`，构造 `Command`，调用 `await agent.PendingCommands.BroadcastAsync(command)`
- `GET /Dashboard/Commands/{clientId}` — 返回 `agent.CommandHistory` 列表视图（或嵌入 Details 页）

**6. Server：Details 页面 UI**

在 Details.cshtml 增加：
- 命令输入 `<textarea>` + 提交表单（POST → `/Dashboard/SendCommand/{id}`）
- 命令历史表格，列：命令文本、ExitCode（绿/红标色）、stdout/stderr 折叠展开、执行时间

**7. Client：重构 `ServerMonitor` + 新增 `AgentChannelService`**

新建 src/Aiursoft.StatHub.Client/Services/AgentChannelService.cs：
- 用 `System.Net.WebSockets.ClientWebSocket` 连接 `ws(s)://{server}/api/agent/channel?clientId={id}`
- **写端**：订阅 dstat 聚合后的 `DstatResult[]`，合并 `SubmitService` 当前的元数据收集逻辑，序列化为 `AgentWsMessage { type: "metrics", payload: MetricsAddressModel }` 并 `SendAsync`
- **读端**：循环 `ReceiveAsync` → `type == "command"` → 启动 `Process("bash", "-c ...")` 执行 shell 命令（超时60s）→ 采集 stdout/stderr/ExitCode → 发送 `AgentWsMessage { type: "command-result", ... }`
- 断连后指数退避（1s → 2s → 4s … 上限 60s）自动重连

在 ServerMonitor.cs 中用 `AgentChannelService` 替换订阅 `SubmitService` 的那条管道（`Subscribe(submitService)` → `Subscribe(agentChannelService)`）。

**8. Client：注册服务 + 移除 `SubmitService`**

在 Startup.cs：
- 删除 `services.AddSingleton<SubmitService>()`
- 新增 `services.AddSingleton<AgentChannelService>()`

**9. SDK：`ServerAccess.MetricsAsync` 可移除**

ServerAccess.cs 删除 `MetricsAsync`（HTTP POST 不再使用），保留 `InfoAsync`（健康检查）。

---

**Verification**

- 单元测试：`AgentChannelService` 消息序列化/反序列化、命令执行结果采集
- 集成测试：仿照 ContainerStatusTests.cs，用 `WebApplicationFactory` 建立 WebSocket 连接，发送 mock metrics 消息并断言 Agent 状态更新；发送命令并断言 CommandHistory 出现结果
- 手动验证：启动 Server + Agent，Dashboard Details 页输入 `echo hello` → 提交 → 刷新页面 → 历史中出现 ExitCode=0、stdout="hello"

---

**Decisions**

- 全 WebSocket 而非混合：消除了 HTTP 轮询、简化连接管理、统一认证边界
- 原生 `System.Net.WebSockets` 用于 Agent 双向信道，`Aiursoft.AiurObserver.WebSocket.Server` 的 pusher 保留用于现有浏览器单向流
- `clientId` 通过 query string 传递而非路径参数，避免与 WebSocket 升级路由冲突
- 历史保留 100 条纯内存，与整体无持久化策略一致

---

这个方案是否符合你的预期？有没有需要调整的地方（比如 shell 命令安全限制、Windows 兼容性、断连行为等）？

User: 非常符合我的预期。但是我问一下，未来可能除了跑命令，还有很多其他需要全双工通信的东西。你有什么高见？

还有就是，比如我快速要求服务器跑两个命令A和B。那么服务器都跑完了，都通过websocket传了回来。我怎么知道哪个是A的结果哪个是B的结果？

除了stdout，还是否传回来stderr、return value？

如果命令花了很长时间，或需要中间交互，现在能否正确处理？？

Gemini: 很好的问题，这几个问题恰好都指向同一个核心设计决策。让我逐一分析。

---

### 问题一：可扩展性

当前信封 `{ "type": "...", "payload": {...} }` 本质上就是一个**消息总线**。未来的扩展（文件传输、日志流、健康探针等）只需注册新的 `type` 和对应 handler。

更进一步，可以正式采用 **JSON-RPC 2.0** 语义：

```json
// Server → Agent (请求，有 id)
{ "jsonrpc": "2.0", "id": "uuid-A", "method": "exec", "params": { "cmd": "apt update" } }

// Agent → Server (响应，回传同一 id)
{ "jsonrpc": "2.0", "id": "uuid-A", "result": { ... } }

// Agent → Server (主动上报，无 id，单向通知)
{ "jsonrpc": "2.0", "method": "metrics", "params": { ... } }
```

JSON-RPC 2.0 的好处是：**请求/响应关联已内置、单向通知也有规范，社区有大量工具支持**，而且未来新增任何 method 都是向后兼容的。

---

### 问题二：命令 A/B 的结果对应

JSON-RPC 的 `id` 字段就是为此设计的。服务器发命令时生成 Guid 作为 `id`，Agent 返回结果时原样带回这个 `id`。服务器侧维护一个 `ConcurrentDictionary<string, TaskCompletionSource<CommandResult>>` 即可精确匹配：

```
Server: id=AAA → exec "echo A"
Server: id=BBB → exec "echo B"
Agent:  id=BBB → result "B"     ← 服务器知道这是 B 的结果
Agent:  id=AAA → result "A"
```

---

### 问题三：stderr / exitCode

计划里已含，三者全带回：`stdout`、`stderr`、`exitCode`。

---

### 问题四：长时间命令 / 流式输出 / 交互

这是最关键的设计分叉点：

| 方案 | 能力 | 复杂度 |
|------|------|--------|
| **等待完成再回传**（当前计划） | 简单，但运行5分钟的命令会阻塞，用户看不到任何中间输出 | 低 |
| **流式 chunk**（推荐第一步） | 进程每产出一行就发一条消息，最后发 done+exitCode，长命令完全可用 | 中 |
| **完整 PTY + stdin 回传**（可选第二步） | 支持 `sudo`、`top`、`vim` 等需要终端的交互命令，配合 xterm.js | 高 |

**推荐分两阶段实现：**

阶段一 方案（流式 chunk）：

```
Server → Agent   { method: "exec",          id: "AAA", params: { cmd: "..." } }
Agent  → Server  { method: "stdout-chunk",  id: "AAA", params: { data: "line1\n" } }
Agent  → Server  { method: "stdout-chunk",  id: "AAA", params: { data: "line2\n" } }
Agent  → Server  { method: "stderr-chunk",  id: "AAA", params: { data: "warn\n" } }
Agent  → Server  { method: "exec-done",     id: "AAA", params: { exitCode: 0 } }

Server → Agent   { method: "exec-cancel",   id: "AAA" }  ← 可随时取消
``` 

Asked a question (命令交互)

好，现在我有完整决策了，来更新计划。

---

## Plan: 全 WebSocket 双向通信（JSON-RPC 2.0 信封 + 流式命令输出）

**设计思路：** 单条持久 WebSocket 连接完全替代 `POST /api/metrics`。协议采用 **JSON-RPC 2.0** 信封，上行（Agent → Server）用 notification 推送 metrics 和流式命令输出，下行（Server → Agent）用 request 下发命令和取消。每条命令用 `id`（Guid）关联，命令执行过程中 stdout/stderr 以 chunk 流式回传，结束时发 `exec-done` 携带 exitCode。未来扩展任何功能只需注册新的 `method`，完全向后兼容。

---

**协议消息清单**

```
// 上行（Agent → Server，notification，无 id）
method: "metrics"        params: MetricsPayload（完整 MetricsAddressModel 内容）
method: "stdout-chunk"   params: { id, data: string }
method: "stderr-chunk"   params: { id, data: string }
method: "exec-done"      params: { id, exitCode: int }

// 下行（Server → Agent，request，有 id）
method: "exec"           id: Guid  params: { cmd: string }
method: "exec-cancel"    id: Guid（取消正在运行的命令）
```

---

**Steps**

**1. SDK：新增协议模型**

在 Models 新增：
- `JsonRpcMessage.cs` — 通用信封：`Jsonrpc`("2.0")、`Id`(string?)、`Method`(string)、`Params`(JToken?) — 上下行共用
- `Command.cs` — `Id`(Guid)、`Text`(string)、`IssuedAt`(DateTime)、`CancelTokenSource`(CancellationTokenSource，内存侧)
- `CommandExecution.cs` — `CommandId`(Guid)、`Text`、`Stdout/Stderr`(StringBuilder，运行中追加)、`ExitCode`(int?)、`StartedAt`/`FinishedAt`(DateTime)、`IsRunning`(bool)

**2. SDK：`Agent` 模型增加命令通道**

在 Agent.cs 新增：
- `AsyncObservable<Command> PendingCommands` — Dashboard 广播命令 → 服务器 WebSocket 读循环监听并下发
- `ConcurrentDictionary<Guid, CommandExecution> CommandHistory` — 按 commandId 存储，最多 100 条（超过时移除最旧的）

**3. Server：`AgentChannelController`**

新建 src/Aiursoft.StatHub/Controllers/AgentChannelController.cs：
- `GET /api/agent/channel?clientId={id}` — `[AllowAnonymous]`、`[EnforceWebSocket]`
- `AcceptWebSocketAsync()` 后启动两个并发 Task（用 `CancellationTokenSource` 联动取消）：

  **读循环**（Agent → Server）：
  - 循环 `ReceiveAsync` → 反序列化 `JsonRpcMessage`
  - `method == "metrics"` → 反序列化 Params 为 `MetricsPayload`，更新 Agent 字段 + Broadcast Stats（地理位置解析同当前 `ApiController` 逻辑），更新 `LastUpdate`
  - `method == "stdout-chunk"` → `CommandHistory[id].Stdout.Append(data)`
  - `method == "stderr-chunk"` → `CommandHistory[id].Stderr.Append(data)`
  - `method == "exec-done"` → 更新 `ExitCode`、`FinishedAt`、`IsRunning = false`

  **写循环**（Server → Agent）：
  - 订阅 `agent.PendingCommands`，逐条将 `Command` 序列化为 `JsonRpcMessage { method: "exec", id: cmd.Id, params: { cmd: cmd.Text } }` 并 `SendAsync`

- 任一侧断连时两个 Task 取消，Agent 的 `LastUpdate` 停止刷新，自然变为 Offline

**4. Server：`DashboardController` 下发与查询**

在 DashboardController.cs 新增：
- `POST /Dashboard/SendCommand/{clientId}` — `[Authorize(CanViewDashboard)]`，构造 `Command`（新建 `CancelTokenSource`），追加到 `CommandHistory`（`IsRunning = true`），调用 `await agent.PendingCommands.BroadcastAsync(command)`
- `POST /Dashboard/CancelCommand/{clientId}/{commandId}` — `[Authorize(CanViewDashboard)]`，调用 `CommandHistory[id].CancelTokenSource.Cancel()`，通过 `PendingCommands` 广播 `exec-cancel`
- `GET /Dashboard/Commands/{clientId}` — 返回 `CommandHistory.Values` 列表视图（或直接嵌入 Details 页的局部视图）

**5. Server：Details 页面 UI**

在 Details.cshtml 增加：
- 命令输入区：`<textarea>` + 提交按钮（POST `/Dashboard/SendCommand/{id}`）
- 命令历史列表：`CommandId`、命令文本、运行状态（🟡运行中 / ✅完成 / ❌失败）、ExitCode、stdout 折叠展开、stderr 折叠展开（红色标注）、耗时
- 运行中的命令显示"取消"按钮

**6. Server：`ApiController` 瘦身**

在 ApiController.cs 删除 `POST /api/metrics`，只保留 `GET /api/info` 健康检查。

**7. Client：`AgentChannelService`（新核心服务）**

新建 src/Aiursoft.StatHub.Client/Services/AgentChannelService.cs，实现 `IConsumer<DstatResult[]>`（供 `ServerMonitor` 订阅）：

- **连接管理**：`ClientWebSocket.ConnectAsync(ws(s)://{server}/api/agent/channel?clientId={id})` ，断连后指数退避（1→2→4…上限60s）自动重连

- **写端**（接收 dstat 聚合结果）：
  - 实现 `Consume(DstatResult[])` — 收集 `SubmitService` 当前所有元数据，序列化为 `JsonRpcMessage { method: "metrics", params: MetricsPayload }`，调用 `SendAsync`
  
- **读端**（在独立 Task 中运行）：
  - 循环 `ReceiveAsync` → 反序列化 `JsonRpcMessage`
  - `method == "exec"` → 启动命令执行子流程（见下）
  - `method == "exec-cancel"` → 取消对应 `id` 的执行 task

- **命令执行子流程**（每条命令独立 Task）：
  
  ```
  Process("bash", "-c cmd") → 
    stdout DataReceived → SendAsync { method: "stdout-chunk", id, data }
    stderr DataReceived → SendAsync { method: "stderr-chunk", id, data }
    WaitForExitAsync(token, timeout:300s) → 
    SendAsync { method: "exec-done", id, exitCode }
  ```
  
  用 `Process.EnableRaisingEvents = true` + `OutputDataReceived` / `ErrorDataReceived` 事件驱动流式输出，**不等待进程结束**才发数据。取消时 `Process.Kill(entireProcessTree: true)`。

**8. Client：`ServerMonitor` 改动**

在 ServerMonitor.cs 将 `.Subscribe(submitService)` 改为 `.Subscribe(agentChannelService)`，其余管道不变。

**9. Client：`Startup.cs` 注册服务**

在 Startup.cs：
- 删除 `services.AddSingleton<SubmitService>()`
- 新增 `services.AddSingleton<AgentChannelService>()`

**10. SDK：清理 `ServerAccess`**

在 ServerAccess.cs 删除 `MetricsAsync`、保留 `InfoAsync`。

---

**Verification**

- 单元测试：`JsonRpcMessage` 序列化/反序列化（含 id 完整回收）、命令流式输出 chunk 拆分
- 集成测试：用 `WebApplicationFactory` 建立 WebSocket 模拟 Agent → 推送 metrics → 断言 Agent 状态更新；服务器下发 `exec` → 模拟 Agent 回 chunk + done → 断言 `CommandHistory` 中 ExitCode 正确
- 手动验证：`echo hello`（短命令）、`for i in $(seq 5); do echo $i; sleep 1; done`（长命令流式）、`cat /nonexistent`（stderr + 非零 ExitCode）

---

**Decisions**

- **JSON-RPC 2.0**：`id` 关联请求/响应，消解 A/B 乱序问题；`method` 字段提供天然扩展点，未来加文件传输、日志订阅等只需新 method 和 handler
- **流式 chunk**而非等待完成：长命令实时可见，不阻塞 WebSocket 写端；`exec-cancel` 支持随时中断
- **不支持 stdin 交互**（阶段一）：覆盖绝大多数运维场景；PTY/stdin 作为后续独立 feature 可在协议层无缝扩展（新增 `stdin-data` method）
- **命令并发**：多条命令可同时执行（各自独立 Task + `id` 隔离），结果乱序返回不影响正确性

User: 很好。开干！

Gemini: 我目前处于 **Plan（规划）模式**，无法直接执行文件编辑操作。

请使用界面上的 **"Start"** / **"Switch to Agent"** 按钮切换到可执行模式，然后我就可以开始动手写代码了。

User: 开始开发！!
