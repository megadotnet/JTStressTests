# JTStressTests - HTTP 并发压力测试工具

> **声明**：本工具仅用于对已获得正式授权的系统进行性能及压力测试。严禁用于任何未授权系统的攻击行为。

---

## 1. 项目简介

`JTStressTests` 是一个基于 .NET 8 构建的高性能轻量级 HTTP 并发压力测试控制台工具。该工具通过高效的连接复用、并发控制和异步 I/O 机制，对目标 HTTP/HTTPS 接口发起高并发压力测试，并提供多维度的实时性能统计报告（包括 QPS、响应延迟百分位、HTTP 状态码分布、异常分类等），同时支持将所有请求细节导出为 CSV 格式供进一步分析。

### 核心特性
- **高性能连接池管理**：基于 `SocketsHttpHandler` 进行连接池配置与请求复用（`PooledConnectionLifetime` 与 `MaxConnectionsPerServer`），支持在单机高并发下稳定发起长连接压测。
- **并发控制与平滑取消**：采用 `SemaphoreSlim` 与 `Task.Run` 工作池协同，支持精准并发限制；支持监听系统 `Ctrl+C` 信号及超时平滑收尾（最大宽限 10 秒）。
- **灵活的配置合并体系**：支持 `appsettings.json` 基础配置、基于 `ASPNETCORE_ENVIRONMENT` 的环境差异化配置（如 `appsettings.Development.json`），以及命令行 CLI 参数覆盖（命令行参数具有最高优先级）。
- **细粒度性能统计**：内置线性插值法计算高精度延迟百分位数（P50、P90、P95、P99），统计 HTTP 状态码分布与异常类型分布。
- **压测数据导出**：支持通过 `--output` 参数将每次请求的时间戳、耗时、状态码、成功标志及异常信息无损导出为 CSV 文件。

---

## 2. 技术栈说明

- **目标运行时**：.NET 8.0 (`net8.0`)
- **编程语言**：C# 12（采用顶级语句 Top-Level Statements 简化程序入口）
- **核心依赖类库**：
  - `System.Net.Http` (`HttpClient`, `SocketsHttpHandler`)：提供底层 HTTP 连接复用与网络请求。
  - `System.Text.Json`：提供高性能 JSON 配置解析。
  - `System.Threading` (`SemaphoreSlim`, `CancellationTokenSource`, `Interlocked`)：实现高并发异步控制。
  - `System.Collections.Concurrent` (`ConcurrentBag<T>`)：提供无锁化高并发请求记录收集。

> **工程实际说明**：
> 当前代码工程为纯 .NET 8 控制台应用，**未包含**前端框架（如 Vue / React / Vite）、Nginx 反向代理配置、Python 后端服务或官方 Docker 镜像编排文件。相关配置说明及容器化部署方案见下文对应章节。

---

## 3. 目录结构

系统已按单一职责原则与面向对象设计（SOLID）重构为领域模块化结构，详细架构与接口规范参见 [REFACTORING.md](./REFACTORING.md)：

```text
.
├── JTStressTests.sln          # Visual Studio / .NET 解决方案文件
├── JTStressTests.csproj       # 项目主工程文件 (net8.0)
├── JTStressTests.Tests/       # 单元测试工程 (MSTest, Moq, Coverlet)
│   ├── JTStressTests.Tests.csproj
│   ├── CommandLineParserTests.cs
│   ├── ConfigLoaderTests.cs
│   ├── RequestBuilderTests.cs
│   ├── StatisticsCalculatorTests.cs
│   ├── CsvExporterTests.cs
│   ├── StressTestEngineTests.cs
│   ├── ModelTests.cs
│   └── RefactoredArchitectureTests.cs
├── Cli/                       # 命令行交互领域模块 (接口、实现与专属模型)
├── Configuration/             # 配置管理领域模块 (接口、实现与专属模型)
├── Http/                      # HTTP 传输与请求构建 (构建者模式与工厂模式)
├── Engine/                    # 并发压测引擎领域模块 (接口与核心引擎实现)
├── Statistics/                # 统计计算与报告输出 (策略模式打印器)
├── Export/                    # 结果持久化导出模块 (策略模式导出器)
├── Services/                  # [兼容垫片] 保持历史服务调用 API 100% 兼容
├── Models/                    # [兼容垫片] 保持历史数据模型定义 100% 兼容
├── Program.cs                 # 程序入口 (依赖倒置与组件协同)
├── run-tests.sh               # 单测与覆盖率自动化一键执行脚本
├── TESTING.md                 # 单元测试与测试覆盖率详细指引文档
├── REFACTORING.md             # 系统重构架构、设计模式与接口定义详细文档
├── appsettings.json           # 默认压测目标与请求头配置文件
├── .gitignore                 # Git 忽略配置
└── README.md                  # 项目说明文档
```

---

## 4. 架构设计与请求调用链路

### 4.1 整体架构设计
`JTStressTests` 采用生产者-工作线程池模式运行于客户端：
1. **参数与配置加载层**：
   - 启动时解析命令行参数（`--concurrency`, `--duration`, `--url`, `--output`, `--method`, `--body`, `--header`）。
   - 自动检测并读取程序运行目录下的 `appsettings.json`。
   - 检测环境变量 `ASPNETCORE_ENVIRONMENT`，若存在对应环境配置文件（如 `appsettings.Production.json`）则读取并合并覆盖。
   - 命令行参数具有最高优先级，最终合成一份全局有效的压测配置 `AppConfig`。
2. **底层 HTTP 传输层**：
   - 使用单例 `HttpClient` 与专属配置的 `SocketsHttpHandler`。
   - `MaxConnectionsPerServer = concurrency`：保证每个服务端的最大连接数与指定并发度匹配，避免客户端连接等待。
   - `PooledConnectionLifetime = TimeSpan.FromMinutes(5)`：保障长连接生命周期内定期重新解析 DNS，适应动态路由。
   - `UseCookies = false`：关闭客户端 Cookie 自动管理，避免并发状态污染，Cookie 完全由配置头传入。
3. **并发压测执行层**：
   - 使用 `SemaphoreSlim(concurrency)` 严格限制在途并发请求数。
   - 启动 `concurrency` 个并行 `Task`，在全局 `CancellationTokenSource` 允许的持续时间（`duration`）内循环获取信号量并执行压测。
   - 每次请求通过 `HttpRequestMessage` 动态构建独立的请求体与 Header，使用 `HttpCompletionOption.ResponseHeadersRead` 快速接收响应头，并通过 `response.Content.CopyToAsync(Stream.Null)` 消费丢弃响应流，确保底层 Socket 连接高效复用。
4. **统计与导出层**：
   - 并发采集结果记录写入线程安全的 `ConcurrentBag<RequestResult>`。
   - 压测结束（超时或用户按下 `Ctrl+C`）后触发平滑退出，汇总计算总请求数、QPS、成功率、耗时（平均、最小、最大、P50/P90/P95/P99 百分位）、HTTP 状态码及异常分布，并按需导出 CSV。

### 4.2 请求调用链路

```text
[启动程序]
   │
   ├─► 解析命令行 CLI 参数
   ├─► 读取 appsettings.json
   ├─► [可选] 根据 ASPNETCORE_ENVIRONMENT 读取 appsettings.{Env}.json
   └─► 合并配置 (CLI > Env Config > Base Config)
         │
         ▼
[初始化 SocketsHttpHandler & HttpClient]
         │
         ▼
[并发 Worker 线程池启动 (Tasks = concurrency)]
   │
   ├── Worker 1 ──┐
   ├── Worker 2 ──┼─► [获取信号量 SemaphoreSlim]
   └── Worker N ──┘            │
                               ▼
                   [构建 HttpRequestMessage]
                               │
                               ▼
                   [发送 HTTP 请求至目标 API]
                               │
                               ▼
                   [读取状态码 & 丢弃流 Stream.Null]
                               │
                               ▼
                   [记录 RequestResult 到 ConcurrentBag]
                               │
                               ▼
                   [释放信号量，开始下一轮请求]
                               │
         ┌─────────────────────┘
         ▼ (达到 duration 或触发 Ctrl+C)
[停止 Worker，平滑收尾 (宽限 10 秒)]
         │
         ▼
[控制台输出统计报告 (QPS, Percentiles, 状态码, 异常)]
         │
         ▼
[可选：写入 CSV 结果文件]
```

---

## 5. 本地开发与运行步骤

### 5.1 环境要求
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 或更高版本。
- 操作系统支持：Linux、macOS、Windows。

### 5.2 启动与运行

1. **还原与构建项目**：
   ```bash
   dotnet build
   ```

2. **执行单元测试与代码覆盖率检查**：
   ```bash
   # 运行一键测试脚本 (生成测试报告与覆盖率文件至 ./TestResults)
   ./run-tests.sh

   # 或通过 dotnet CLI 运行
   dotnet test
   ```
   详细测试规范与覆盖率说明参见 [TESTING.md](./TESTING.md)。

3. **快速直接运行（使用 appsettings.json 默认配置）**：
   ```bash
   dotnet run
   ```

3. **通过命令行指定参数运行**：
   ```bash
   dotnet run -- --url "https://api.example.com/v1/test" --concurrency 100 --duration 30 --method POST --body '{"test":1}'
   ```

4. **发布为单文件独立可执行文件（推荐用于生产压测机）**：
   ```bash
   # 以 Linux x64 为例
   dotnet publish -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true -o ./publish
   cd ./publish
   ./JTStressTests --url "https://api.example.com/v1/test" --concurrency 200 --duration 60 --output result.csv
   ```

### 5.3 命令行参数详解

| 参数名称 | 类型 | 默认值 | 说明 |
| :--- | :--- | :--- | :--- |
| `--url` | string | 从配置文件读取 | 压测目标 URL 地址（必须指定或配置在 json 中） |
| `--concurrency` | int | `500` | 并发 Worker 数量（必须为正整数） |
| `--duration` | int | `60` | 压测持续时间（秒，必须为正整数） |
| `--method` | string | `POST` | HTTP 请求方法，例如 `GET`, `POST`, `PUT` 等 |
| `--body` | string | 从配置文件读取 | HTTP 请求体内容 |
| `--header` | string | 从配置文件读取 | 自定义请求头，格式为 `Name: Value`，可重复传入多次 |
| `--output` | string | 空 (不导出) | 压测结果 CSV 导出的文件路径 |
| `--help`, `-h` | - | - | 打印帮助信息 |

*注：支持 `--concurrency 200` 或 `--concurrency=200` 两种传参方式。*

---

## 6. 配置文件说明与环境隔离

配置文件位于根目录下 `appsettings.json`，格式如下：

```json
{
  "Url": "https://example.com/api/test-endpoint",
  "Body": "param1=value1&param2=value2",
  "ContentType": "application/x-www-form-urlencoded",
  "Headers": {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/157.0.0.0 Safari/537.36",
    "Accept": "application/json, text/plain, */*",
    "Origin": "https://example.com",
    "Referer": "https://example.com",
    "Cookie": "SESSIONID=xxxxxxxx"
  }
}
```

### 多环境隔离配置
程序原生支持基于 `ASPNETCORE_ENVIRONMENT` 环境变量的多环境隔离机制：
- 若设置环境变量 `export ASPNETCORE_ENVIRONMENT=Staging`；
- 程序将尝试寻找并读取 `appsettings.Staging.json`；
- `appsettings.Staging.json` 中配置的项将覆盖 `appsettings.json` 中的同名项；
- 最终命令行传入的参数会覆盖所有配置文件中的同名项。

---

## 7. 容器化部署与 Docker / docker-compose 指南

由于本工程为压测客户端工具，在服务器端进行容器化运行或分布式批量压测时，可通过以下标准化方式进行封装和部署。

### 7.1 Dockerfile 参考

在项目根目录下创建 `Dockerfile`：

```dockerfile
# 阶段 1: 构建
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["JTStressTests.csproj", "./"]
RUN dotnet restore "JTStressTests.csproj"
COPY . .
RUN dotnet publish "JTStressTests.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 阶段 2: 运行
FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS final
WORKDIR /app
COPY --from=build /app/publish .

# 默认入口
ENTRYPOINT ["dotnet", "JTStressTests.dll"]
CMD ["--help"]
```

#### 构建与运行容器
```bash
# 1. 构建 Docker 镜像
docker build -t jt-stress-tests:latest .

# 2. 运行容器执行压测并挂载输出目录保存 CSV
docker run --rm \
  -v $(pwd)/output:/app/output \
  jt-stress-tests:latest \
  --url "https://api.example.com/test" \
  --concurrency 200 \
  --duration 30 \
  --output /app/output/report.csv
```

### 7.2 docker-compose.yaml 使用参考

在分布式压测场景下，可以使用 `docker-compose.yaml` 编排多个压测节点：

```yaml
version: '3.8'

services:
  stress-node-1:
    build:
      context: .
      dockerfile: Dockerfile
    command: ["--url", "https://api.example.com/test", "--concurrency", "250", "--duration", "60", "--output", "/output/node1.csv"]
    volumes:
      - ./output:/output
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    restart: "no"

  stress-node-2:
    build:
      context: .
      dockerfile: Dockerfile
    command: ["--url", "https://api.example.com/test", "--concurrency", "250", "--duration", "60", "--output", "/output/node2.csv"]
    volumes:
      - ./output:/output
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    restart: "no"
```

#### 启动命令
```bash
# 构建并启动多节点压测
docker compose up --build
```

---

## 8. 运行与部署注意事项

1. **合法合规性与安全授权**：
   - 压测行为会对目标服务器、数据库、反向代理（Nginx）、网关造成巨大负载，甚至导致系统不可用。
   - 务必在已获得业务主管与系统运维团队明确授权并限定窗口期内进行测试。
2. **压测机操作系统参数调优（ulimit 与端口耗尽）**：
   - 在高并发（如并发 500+）短时间内可能创建大量 Socket 连接。若出现 `SocketException` 或连接重置，需检查 Linux 系统的文件打开数限制：
     ```bash
     ulimit -n 65535
     ```
   - 调优本地可用临时端口范围与 TIME_WAIT 快速回收配置：
     ```bash
     sudo sysctl -w net.ipv4.ip_local_port_range="1024 65535"
     sudo sysctl -w net.ipv4.tcp_tw_reuse=1
     ```
3. **内存与结果记录控制**：
   - 工具使用内存中的 `ConcurrentBag<RequestResult>` 记录每次请求。如果在数万级 QPS 下进行数十分钟的超长压测，可能会产生数千万条对象记录导致高内存占用。超长时间极限压测建议合理规划持续时间（`--duration`）或分批次压测。
4. **网络带宽与客户端瓶颈**：
   - 在压测吞吐量达不到预期时，应排查压测客户端本身的 CPU 利用率（是否打满）、网卡出口带宽以及 DNS 解析耗时，确保客户端没有形成瓶颈。
