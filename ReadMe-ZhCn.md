# JTStressTests - 高性能 HTTP 并发压力测试工具

[![.NET Version](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![Language](https://img.shields.io/badge/Language-C%23%2012-brightgreen.svg)](https://learn.microsoft.com/zh-cn/dotnet/csharp/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Unit Tests](https://img.shields.io/badge/Tests-74%20Passed-success.svg)](./TESTING.md)

[English](./README.md) | [简体中文 (Chinese Version)](./ReadMe-ZhCn.md)

> **法律声明与免责提示**：本工具仅限用于对已获得正式书面授权的自有或被测系统进行性能、基准及压力测试。严禁将本工具用于任何未授权系统的网络攻击、恶意泛洪或非法渗透行为。

---

## 1. 项目简介

`JTStressTests` 是一个基于 .NET 8 与 C# 12 构建的高性能、轻量级、全异步 HTTP 并发与压力测试命令行（CLI）工具。该工具专为现代后端微服务与 Web API 设计，具备极高的资源利用率与吞吐能力，能够帮助开发人员、架构师与 DevOps 团队在单机或多节点分布式环境中精准评估服务端的承载瓶颈与延迟特性。

### 核心特性
- **高性能底层连接池管理**：深度基于 .NET 底层 `SocketsHttpHandler` 进行连接池配置与长连接复用（`PooledConnectionLifetime = 5 分钟`，`MaxConnectionsPerServer = 并发数`），杜绝极端高并发压测时客户端本地端口与 Socket 套接字耗尽问题。
- **精准的并发控制与无锁流转**：结合 `SemaphoreSlim` 信号量与 .NET Task 线程池，在严格限定在途最大请求数的同时避免上下文过度切换；采用无锁化内存集合保障超高并发记录采集。
- **平滑优雅终止与系统信号感知**：实时监听系统中断信号（`Ctrl+C` / SIGINT）与预设超时时间，平滑等待在途请求收尾（最长 10 秒宽限期），避免压测数据截断失真。
- **多维度细粒度统计指标**：内置高精度连续线性插值算法计算延迟百分位数（P50、P90、P95、P99），实时统计 QPS 吞吐量、成功率、HTTP 响应状态码分布以及底层网络异常分类。
- **灵活的层级化多环境配置合并**：原生支持 `appsettings.json` 基础配置、基于 `ASPNETCORE_ENVIRONMENT` 环境变量的环境特定配置（如 `appsettings.Staging.json`），以及命令行参数最高优先级覆盖。
- **开箱即用的数据持久化导出**：支持通过 `--output` 参数将请求级耗时、状态码、时间戳与异常信息安全无损导出为 CSV 文件；采用策略模式设计，未来可无缝拓展 JSON、Parquet 等更多格式。

---

## 2. 技术栈清单

本项目采用了严谨的 .NET 现代技术栈，无臃肿第三方外部网络包依赖。按**前端**、**后端**、**基础设施**、**工具链**四大类分类梳理如下：

### 2.1 前端与用户交互 (Frontend & UI)
| 组件 / 框架 | 具体版本号 | 在项目中的核心作用说明 |
| :--- | :--- | :--- |
| **控制台终端交互引擎** | System Console (.NET 8.0) | 提供跨平台 CLI 终端用户界面，支持实时压测启动提示、交互状态流转以及 ANSI 结构化测试报告表格输出，免去沉重的 GUI 前端框架开销。 |
| **命令行参数解析组件** | 自主架构 (`JTStressTests.Cli`) | 高性能零外部依赖参数解析引擎，支持 POSIX 规范短参/长参、键值分离语法（`--flag value` 与 `--flag=value`）、布尔标志与多值数组参数解析（如多次传递自定义 Header）。 |

### 2.2 后端语言及运行环境 (Backend & Runtime)
| 核心组件 / 类库 | 具体版本号 | 在项目中的核心作用说明 |
| :--- | :--- | :--- |
| **核心运行时平台** | .NET Runtime 8.0 (`net8.0`) | 跨平台托管运行环境，提供原生 JIT 编译器优化、高效的分代 GC 垃圾回收机制以及分层 PGO（Profile-Guided Optimization）运行时加速。 |
| **开发编程语言** | C# 12.0 (`LangVersion: latest`) | 核心开发语言，采用顶级语句（Top-level statements）、空安全检查（Nullable）、模式匹配（Pattern Matching）及记录类型（Records）提升代码表现力。 |
| **HTTP 传输子系统** | `System.Net.Http` (.NET 8.0 BCL) | 核心通信基石，提供 `HttpClient`、`HttpRequestMessage` 与流式读取优化（`HttpCompletionOption.ResponseHeadersRead` 与 `CopyToAsync(Stream.Null)`）。 |
| **JSON 序列化解析引擎** | `System.Text.Json` (.NET 8.0 BCL) | 高性能零内存分配 UTF-8 JSON 解析与反序列化组件，负责多级配置文件的加载、校验与级联属性合并。 |
| **多线程与异步并发基元** | `System.Threading` (.NET 8.0 BCL) | 提供 `SemaphoreSlim`、`CancellationTokenSource` 与 `Interlocked` 原子操作，确保超高并发压测时工作线程精准调度。 |
| **无锁化并发数据集合** | `System.Collections.Concurrent` (.NET 8.0 BCL) | 提供高性能线程安全内存集合 `ConcurrentBag<RequestResult>`，在高并发多线程请求结果写入时不产生全局互斥锁竞争。 |
| **领域架构抽象层** | 自主研发核心抽象 | 抽离策略模式（`IResultExporter`, `IReportPrinter`）、工厂模式（`IHttpClientFactory`）与构建者模式（`IRequestBuilder`），实现高度解耦。 |

### 2.3 基础设施与第三方中间件 (Infrastructure & Middleware)
| 组件 / 系统 | 具体版本号 | 在项目中的核心作用说明 |
| :--- | :--- | :--- |
| **HTTP 连接池处理器** | `SocketsHttpHandler` (.NET 8.0) | 底层 Socket 连接池管控器，配置 `PooledConnectionLifetime`（5分钟自动重连兼顾 DNS 刷新）与 `MaxConnectionsPerServer`，关闭默认 Cookie 避免线程间污染。 |
| **内存管道缓冲系统** | 托管内存池 (`ConcurrentBag<T>`) | 内存化高通量压测流水线结果缓冲区，避免压测过程中频繁写入本地磁盘或数据库导致吞吐量瓶颈。 |
| **操作系统 TCP/IP 协议栈** | POSIX / WinSock 协议栈 | 底层网络内核传输支撑，通过调优 Linux `ulimit`、`tcp_tw_reuse` 与本地临时端口范围支持万级并发连接。 |
| **容器化运行引擎 (可选)** | Docker Engine >= 20.10 | 提供基于 Alpine Linux 的极小多阶段构建 Docker 镜像，满足无 .NET 环境服务器的隔离压测运行。 |
| **容器集群编排 (可选)** | Docker Compose >= 2.0 | 支持定义多压测节点分布式并行压测集群配置，模拟大规模并发负载。 |

### 2.4 构建与测试工具链 (Toolchain & Testing)
| 工具 / 类库 | 具体版本号 | 在项目中的核心作用说明 |
| :--- | :--- | :--- |
| **项目构建与编译系统** | .NET SDK 8.0 / MSBuild 17.8+ | 负责工程依赖还原、代码静态编译检查、跨平台二进制分发以及自包含独立单文件发布。 |
| **单元测试核心框架** | `MSTest` 4.0.1 | 微软标准化测试框架，支持 `[TestClass]`, `[TestMethod]`, `[DataRow]` 参数化测试套件及断言验证。 |
| **依赖注入与 Mock 框架** | `Moq` 4.21.0 | 模拟底层 `HttpMessageHandler` 与业务接口，实现纯离线确定性单测（网络失败、超时与极值分支覆盖）。 |
| **单测代码覆盖率收集器** | `coverlet.collector` 10.1.0 | 跨平台单测覆盖率收集探针，自动输出标准 Cobertura 与 OpenCover 覆盖率格式报告。 |
| **自动化测试一键脚本** | Bash Shell 脚本 (`run-tests.sh`) | 自动化执行全部 74 项单元测试并输出报告至 `./TestResults` 目录的一键跨平台脚本。 |
| **版本控制系统** | Git 2.x+ | 代码版本管理、变更追踪与提交审查。 |

---

## 3. 环境依赖要求

在克隆和运行本项目前，请确保开发与运行主机满足以下最低软硬件兼容版本要求：

| 依赖组件 | 最低兼容版本 | 推荐版本 | 作用说明 |
| :--- | :--- | :--- | :--- |
| **.NET SDK** | `8.0.100` | `8.0.x` 或 `10.0.x` LTS | 编译源码、开发调试、执行单元测试必须安装。 |
| **.NET Runtime** | `8.0.0` | `8.0.x` | 仅当运行非自包含（Framework-dependent）发布程序时需要。 |
| **操作系统 (OS)** | | | 支持主流操作系统： |
| - Linux | Kernel 3.10+ (glibc 2.23+ 或 musl 1.2+) | Ubuntu 20.04+, Debian 11+, RHEL 8+, Alpine 3.18+ | 生产级压测主机首选环境。 |
| - macOS | macOS 11.0 Big Sur (x64 / arm64) | macOS 13 Ventura 及以上 (Apple Silicon / Intel) | 适配日常开发与调试。 |
| - Windows | Windows 10 (1607+) / Windows Server 2016+ | Windows 11 / Windows Server 2022 (x64 / arm64) | 原生支持 Visual Studio 与 Windows 运行。 |
| **Docker (可选)** | `20.10.0` | `24.0.x`+ | 用于容器化构建与无依赖执行。 |
| **Docker Compose (可选)** | `2.0.0` | `v2.20.0`+ | 用于多节点协同分布式压测。 |

---

## 4. 本地部署与启动步骤

以下操作指令已适配主流的 **Windows**、**macOS** 与 **Linux** 开发环境，可直接复制执行。

### 4.1 检出代码与项目编译

```bash
# 1. 克隆代码仓库
git clone https://github.com/your-org/JTStressTests.git
cd JTStressTests

# 2. 还原 NuGet 依赖并编译项目 (全平台通用)
dotnet build
```

### 4.2 执行单元测试与覆盖率验证

```bash
# 方式 A：通过跨平台测试脚本一键运行 (Linux / macOS / Git Bash)
chmod +x run-tests.sh
./run-tests.sh

# 方式 B：通过 dotnet CLI 标准命令执行 (Windows / macOS / Linux)
dotnet test

# 方式 C：执行测试并生成代码覆盖率文件
dotnet test --collect:"XPlat Code Coverage"
```

### 4.3 本地直接启动压测

```bash
# 使用 appsettings.json 中的默认配置启动
dotnet run

# 通过 CLI 参数覆盖运行简单的 GET 压测 (Windows PowerShell / macOS / Linux)
dotnet run -- --url "https://api.example.com/v1/health" --concurrency 50 --duration 10 --method GET

# 执行携带请求体、自定义 Header 并导出 CSV 的高级 POST 压测
dotnet run -- \
  --url "https://api.example.com/v1/login" \
  --concurrency 200 \
  --duration 30 \
  --method POST \
  --body '{"username":"tester","password":"password123"}' \
  --header "Authorization: Bearer my-jwt-token" \
  --header "X-Request-Source: StressTest" \
  --output ./test-result.csv
```

### 4.4 编译为生产级自包含独立单文件 (无需安装 .NET 运行时)

对于压测机环境，强烈建议将其编译为免安装运行时的独立单文件可执行程序：

#### Linux (x64 / ARM64)
```bash
# Linux x64 发布
dotnet publish JTStressTests.csproj -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true -o ./publish-linux
cd ./publish-linux
chmod +x ./JTStressTests
./JTStressTests --url "https://api.example.com/api" --concurrency 500 --duration 60 --output result.csv
```

#### macOS (Apple Silicon arm64 / Intel x64)
```bash
# macOS Apple Silicon (M1/M2/M3/M4)
dotnet publish JTStressTests.csproj -c Release -r osx-arm64 --self-contained true /p:PublishSingleFile=true -o ./publish-macos
cd ./publish-macos
chmod +x ./JTStressTests
./JTStressTests --url "https://api.example.com/api" --concurrency 200 --duration 30
```

#### Windows (PowerShell / CMD)
```powershell
# Windows x64 发布
dotnet publish JTStressTests.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o .\publish-win
cd .\publish-win
.\JTStressTests.exe --url "https://api.example.com/api" --concurrency 300 --duration 45 --output result.csv
```

### 4.5 容器化部署启动 (Docker & Docker Compose)

#### Docker 独立镜像运行
```bash
# 1. 编译镜像
docker build -t jt-stress-tests:latest .

# 2. 挂载本地 output 目录并执行压测
docker run --rm \
  -v "$(pwd)/output:/app/output" \
  jt-stress-tests:latest \
  --url "https://api.example.com/test" \
  --concurrency 250 \
  --duration 60 \
  --output /app/output/report.csv
```

#### Docker Compose 多节点编排启动
```bash
# 启动多节点分布式压测集群
docker compose up --build
```

---

## 5. 命令行参数详细规范

程序支持以空格（`--flag value`）或等号（`--flag=value`）传参，命令行参数优先级高于任何配置文件：

| 参数名称 | 数据类型 | 默认值 | 详细说明 |
| :--- | :--- | :--- | :--- |
| `--url` | 字符串 | 优先读取 `appsettings.json` | 待压测的目标 HTTP/HTTPS URL 接口地址。 |
| `--concurrency` | 整数 | `500` | 客户端并发工作任务数量（必须为正整数）。 |
| `--duration` | 整数 | `60` | 压测持续时间，单位：秒（必须为正整数）。 |
| `--method` | 字符串 | `POST` | HTTP 请求方法，例如 `GET`, `POST`, `PUT`, `DELETE`, `PATCH` 等。 |
| `--body` | 字符串 | 优先读取 `appsettings.json` | 请求载荷内容（支持 JSON 文本或表单序列化字符串）。 |
| `--header` | 字符串 | 优先读取 `appsettings.json` | 自定义请求头，格式为 `Name: Value`，支持多次传入叠加。 |
| `--output` | 字符串 | 空（不导出） | 压测全量明细结果导出的 CSV 文件目标路径。 |
| `--help`, `-h` | 标志 | `false` | 打印使用帮助和命令行参数说明。 |

---

## 6. 项目模块结构说明

项目遵循 SOLID 原则，按垂直业务领域划分各核心模块，同时通过轻量级垫片维持 100% 向后兼容：

```text
.
├── JTStressTests.sln                  # 解决方案入口
├── JTStressTests.csproj               # 主程序工程配置文件 (.NET 8.0)
├── Program.cs                         # 程序唯一主入口 (顶级语句、依赖倒置与领域协同)
├── appsettings.json                   # 基础配置文件 (默认测试接口、Body 与 Headers)
├── run-tests.sh                       # 自动化单测与覆盖率执行脚本
├── README.md                          # 规范英文版文档
├── ReadMe-ZhCn.md                     # 规范中文版文档 (当前文件)
├── TESTING.md                         # 单元测试规范与覆盖率详细指南
├── REFACTORING.md                     # 系统重构架构、设计模式与接口定义规范
│
├── Cli/                               # 命令行交互领域模块
│   ├── ICommandLineParser.cs          # 命令行解析抽象接口
│   ├── CommandLineParser.cs           # 分词解析器实现 (支持多值与等号解析)
│   └── CommandLineOptions.cs          # 命令行输入参数专属模型
│
├── Configuration/                     # 配置管理领域模块
│   ├── IConfigLoader.cs               # 配置加载与合并抽象接口
│   ├── ConfigLoader.cs                # 多环境 JSON 加载与级联覆盖合并实现
│   └── AppConfig.cs                   # 配置文件专属 POCO 模型
│
├── Http/                              # 网络传输与构建模块
│   ├── IRequestBuilder.cs             # 请求构建器接口 (构建者模式)
│   ├── RequestBuilder.cs              # HttpRequestMessage 组装实现
│   ├── IHttpClientFactory.cs          # HTTP 客户端工厂接口 (工厂模式)
│   └── SocketsHttpClientFactory.cs    # 基于 SocketsHttpHandler 的高性能长连接工厂
│
├── Engine/                            # 并发压测执行引擎模块
│   ├── IStressTestEngine.cs           # 压测执行引擎抽象接口
│   ├── StressTestEngine.cs            # 基于 SemaphoreSlim 的高并发压测执行器
│   └── RequestResult.cs               # 单次请求执行结果数据记录
│
├── Statistics/                        # 性能统计与报告输出模块
│   ├── IStatisticsCalculator.cs       # 性能指标计算器接口
│   ├── StatisticsCalculator.cs        # 线性插值百分位数 (P50/P90/P95/P99) 计算实现
│   ├── IReportPrinter.cs              # 报告打印策略接口 (策略模式)
│   ├── ConsoleReportPrinter.cs        # 控制台结构化表格报告打印实现
│   └── StressTestReport.cs            # 压测统计汇总报告领域实体
│
├── Export/                            # 压测结果持久化导出模块
│   ├── IResultExporter.cs             # 导出策略接口 (策略模式)
│   └── CsvResultExporter.cs           # CSV 格式安全转义导出实现
│
├── Services/                          # [历史兼容垫片层]
│   ├── CommandLineParser.cs           # 静态转发至 Cli 领域
│   ├── ConfigLoader.cs                # 静态转发至 Configuration 领域
│   ├── RequestBuilder.cs              # 静态转发至 Http 领域
│   ├── StressTestEngine.cs            # 转发至 Engine 领域
│   ├── StatisticsCalculator.cs        # 静态转发至 Statistics 领域
│   └── CsvExporter.cs                 # 静态转发至 Export 领域
│
├── Models/                            # [历史模型兼容层]
│   ├── AppConfig.cs                   # 隐式类型转换垫片
│   ├── CommandLineOptions.cs          # 隐式类型转换垫片
│   ├── RequestResult.cs               # 隐式类型转换垫片
│   └── StressTestReport.cs            # 隐式类型转换垫片
│
└── JTStressTests.Tests/               # 单元测试工程 (.NET 8.0)
    ├── JTStressTests.Tests.csproj     # 测试工程配置 (MSTest 4.0.1, Moq 4.21.0, Coverlet 10.1.0)
    ├── CommandLineParserTests.cs      # CLI 参数极值、缺省值、畸形格式单测
    ├── ConfigLoaderTests.cs           # JSON 解析、多环境继承、优先级合并单测
    ├── RequestBuilderTests.cs         # 方法映射、UTF-8 编码、Header 防冲突单测
    ├── StatisticsCalculatorTests.cs   # 线性插值百分位算法与 QPS 统计单测
    ├── CsvExporterTests.cs            # CSV 换行/双引号转义与排序单测
    ├── StressTestEngineTests.cs       # 基于 Mock HttpMessageHandler 模拟并发与取消单测
    ├── ModelTests.cs                  # 数据模型不可变性与赋值完整性单测
    └── RefactoredArchitectureTests.cs # 面向接口与设计模式实现的架构行为单测
```

---

## 7. 开发规范与代码设计指南

为了保持代码库的长期健壮性与高质量，所有参与开发与扩展的代码需严格遵守以下规范：

### 7.1 面向对象设计与编码准则
- **严格遵循 SOLID 原则**：
  - **单一职责原则 (SRP)**：每个类仅负责单一关注点（如 `StatisticsCalculator` 仅负责数学统计，不参与控制台输出；控制台输出由 `ConsoleReportPrinter` 负责）。
  - **开闭原则 (OCP)**：如需支持导出 JSON 或 Prometheus 格式，应实现 `IResultExporter` 接口，禁止修改现有调用方代码。
  - **依赖倒置原则 (DIP)**：核心流程依赖接口而非具体类，支持通过依赖注入或 Mock 进行单元测试。
- **现代化 C# 编码规范**：
  - 全局启用可空引用类型（`<Nullable>enable</Nullable>`），杜绝未受保护的 `NullReferenceException`。
  - 异步代码全程使用 `async` / `await`，严禁在异步链条中调用 `.Result` 或 `.Wait()`，避免线程死锁。
  - 保持网络流快速消费释放：对响应体读取使用 `HttpCompletionOption.ResponseHeadersRead` 搭配 `response.Content.CopyToAsync(Stream.Null)`，确保 Socket 及时返还连接池。

### 7.2 单元测试规范 (AAA Pattern)
- 所有测试用例严格遵循 **Arrange-Act-Assert (准备-执行-断言)** 经典模式。
- 测试方法命名遵循统一规范：`MethodUnderTest_StateUnderTest_ExpectedBehavior`（例如：`Percentile_EvenNumberOfElements_CalculatesLinearInterpolation`）。
- 外部网络与 I/O 必须通过 `Moq` 模拟，保障测试在断网环境下 100% 毫秒级稳定通过。
- 核心业务逻辑模块的代码分支与行覆盖率必须保持在 **95%+**。

---

## 8. 常见问题排查与优化 (FAQ)

### 8.1 极端高并发下报 `SocketException: Address already in use` 或连接重置
- **原因分析**：在高并发（如 1000+ 并发度）压测场景下，操作系统可能耗尽本地可用临时端口（Ephemeral Ports）或触发最大文件描述符限制。
- **排查与调优方案**：
  1. 提升 Linux 文件打开数限制：
     ```bash
     ulimit -n 65535
     ```
  2. 优化本地可用端口范围与 TIME_WAIT 套接字快速回收（修改 `/etc/sysctl.conf`）：
     ```bash
     sudo sysctl -w net.ipv4.ip_local_port_range="1024 65535"
     sudo sysctl -w net.ipv4.tcp_tw_reuse=1
     ```

### 8.2 超长压测期间客户端内存持续上升
- **原因分析**：为了在测试结束后准确计算 P50/P90/P95/P99 延迟百分位数及输出全量 CSV，引擎在内存中使用 `ConcurrentBag<RequestResult>` 记录每次请求的原始元数据。当 QPS 达到数万级且压测持续数十分钟时，记录对象将达到千万级，从而增加垃圾回收（GC）压力与内存占用。
- **排查与解决建议**：
  - 合理控制单次连续压测时长（建议每次 60 ~ 300 秒）。
  - 若需超长稳定性压测，建议按批次分段执行。

### 8.3 压测目标域名 IP 变更后客户端仍请求旧节点
- **原因分析**：长连接池复用导致客户端未重新发起 DNS 解析。
- **解决方案**：
  - 本工具在 `SocketsHttpClientFactory` 中内置了 `PooledConnectionLifetime = TimeSpan.FromMinutes(5)`，每隔 5 分钟连接池将平滑淘汰旧连接并重新解析 DNS，无需手动干预。

### 8.4 配置文件中的修改未生效
- **原因分析**：配置层级覆盖优先级规则未明确。
- **优先级次序**：
  $$\text{CLI 命令行参数} > \text{appsettings.\{ASPNETCORE\_ENVIRONMENT\}.json} > \text{appsettings.json}$$
- 检查是否存在同名命令行参数传入，或检查当前终端是否导出了 `ASPNETCORE_ENVIRONMENT` 环境变量导致读取了差异化配置文件。

---

## 9. 开源许可证

本项目基于 [MIT License](LICENSE) 开源许可证发布。
