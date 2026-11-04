# 系统重构交付报告 (Refactoring Architecture & Specification)

## 1. 重构概述与目标达成情况

本项目对 `JTStressTests` 控制台压测工具进行了全面的系统性架构重构。本次重构严格遵循单一职责原则（SRP）、开闭原则（OCP）、依赖倒置原则（DIP）等面向对象 SOLID 核心原则，达成如下核心要求：
1. **代码拆分重构**：将原本平铺在 `Services/` 和 `Models/` 的聚合类全部解耦并迁移至垂直功能领域文件夹（`Cli/`, `Configuration/`, `Http/`, `Statistics/`, `Export/`, `Engine/`），每个文件夹仅保留其核心实现、抽象接口及专属依赖模型。
2. **面向对象设计与模式优化**：抽离出统一的业务逻辑抽象接口，并应用策略模式（结果导出策略、报告打印策略）、工厂模式（HttpClient 与连接池工厂）、构建器模式（HttpRequestMessage 构建），使得系统高度解耦、可替换、可测试与易扩展。
3. **100% 向后兼容**：在原 `Services/` 与 `Models/` 命名空间中保留兼容垫片与隐式类型转换，确保原有公共 API、CLI 命令交互及现有测试用例无破坏性变更。
4. **单元测试与质量保障**：原有 68 个单元测试无缝通过，新增 6 个面向新接口与架构的测试，累计 74 个单测 100% 通过。

---

## 2. 重构后的完整工程目录结构说明

```text
.
├── JTStressTests.sln                  # 解决方案文件
├── JTStressTests.csproj               # 主工程文件 (.NET 8)
├── Program.cs                         # 程序入口（面向接口与依赖倒置协同）
├── appsettings.json                   # 默认压测配置
├── run-tests.sh                       # 自动化测试与覆盖率脚本
├── README.md                          # 项目说明文档
├── TESTING.md                         # 单元测试指引文档
├── REFACTORING.md                     # [新增] 重构交付与架构文档
│
├── Cli/                               # 命令行交互领域模块
│   ├── ICommandLineParser.cs          # 命令行解析抽象接口
│   ├── CommandLineParser.cs           # 命令行解析器具体实现
│   └── CommandLineOptions.cs          # 命令行解析选项专属数据模型
│
├── Configuration/                     # 配置加载与管理领域模块
│   ├── IConfigLoader.cs               # 配置加载与合并抽象接口
│   ├── ConfigLoader.cs                # 配置加载与级联覆盖具体实现
│   └── AppConfig.cs                   # 配置文件专属数据模型
│
├── Http/                              # 底层 HTTP 传输与消息构建领域模块
│   ├── IRequestBuilder.cs             # 请求构建器抽象接口 (构建者模式)
│   ├── RequestBuilder.cs              # 请求构建器具体实现
│   ├── IHttpClientFactory.cs          # HTTP 客户端工厂接口 (工厂模式)
│   └── SocketsHttpClientFactory.cs    # 基于 SocketsHttpHandler 的工厂实现
│
├── Engine/                            # 并发压测执行引擎领域模块
│   ├── IStressTestEngine.cs           # 压测执行引擎抽象接口
│   ├── StressTestEngine.cs            # 高并发压测引擎实现 (基于 SemaphoreSlim)
│   └── RequestResult.cs               # 单次请求执行结果领域模型
│
├── Statistics/                        # 性能指标统计与报告领域模块
│   ├── IStatisticsCalculator.cs       # 统计指标计算器抽象接口
│   ├── StatisticsCalculator.cs        # 百分位线性插值与聚合计算实现
│   ├── IReportPrinter.cs              # 报告输出策略接口 (策略模式)
│   ├── ConsoleReportPrinter.cs        # 控制台格式化报告输出策略实现
│   └── StressTestReport.cs            # 压测统计汇总报告领域模型
│
├── Export/                            # 压测数据导出领域模块
│   ├── IResultExporter.cs             # 导出器策略接口 (策略模式)
│   └── CsvResultExporter.cs           # CSV 格式导出策略实现
│
├── Services/                          # [兼容垫片层] 保持与历史代码及调用方 100% 兼容
│   ├── CommandLineParser.cs
│   ├── ConfigLoader.cs
│   ├── CsvExporter.cs
│   ├── RequestBuilder.cs
│   ├── StatisticsCalculator.cs
│   └── StressTestEngine.cs
│
├── Models/                            # [兼容垫片层] 保持与历史模型定义 100% 兼容
│   ├── AppConfig.cs
│   ├── CommandLineOptions.cs
│   ├── RequestResult.cs
│   └── StressTestReport.cs
│
└── JTStressTests.Tests/               # 单元测试工程
    ├── CommandLineParserTests.cs
    ├── ConfigLoaderTests.cs
    ├── RequestBuilderTests.cs
    ├── StatisticsCalculatorTests.cs
    ├── CsvExporterTests.cs
    ├── StressTestEngineTests.cs
    ├── ModelTests.cs
    └── RefactoredArchitectureTests.cs # [新增] 针对新抽象接口与设计模式的单元测试
```

---

## 3. 统一业务逻辑抽象接口定义文档

### 3.1 命令行模块接口 (`JTStressTests.Cli`)
```csharp
namespace JTStressTests.Cli;

public interface ICommandLineParser
{
    /// <summary>
    /// 解析传入的命令行参数数组
    /// </summary>
    CommandLineOptions Parse(string[] args);

    /// <summary>
    /// 打印命令行使用帮助信息
    /// </summary>
    void PrintUsage();
}
```

### 3.2 配置模块接口 (`JTStressTests.Configuration`)
```csharp
namespace JTStressTests.Configuration;

public interface IConfigLoader
{
    AppConfig? LoadConfig(string path);
    AppConfig? LoadMergedConfig(string baseDirectory, string? environment = null);
    AppConfig Merge(AppConfig? baseConfig, AppConfig overrideConfig);
    (string TargetUrl, string RequestBody, string? RequestContentType, Dictionary<string, string> RequestHeaders) ResolveEffectiveConfig(
        AppConfig? config,
        CommandLineOptions cliOptions);
}
```

### 3.3 HTTP 传输模块接口 (`JTStressTests.Http`)
```csharp
namespace JTStressTests.Http;

/// <summary>
/// 请求构建抽象 (构建者模式)
/// </summary>
public interface IRequestBuilder
{
    HttpRequestMessage BuildRequest(
        string method,
        string targetUrl,
        string requestBody,
        string? requestContentType,
        IReadOnlyDictionary<string, string> requestHeaders);
}

/// <summary>
/// HTTP 客户端与连接池工厂抽象 (工厂模式)
/// </summary>
public interface IHttpClientFactory
{
    HttpClient CreateClient(int concurrency);
}
```

### 3.4 压测引擎模块接口 (`JTStressTests.Engine`)
```csharp
namespace JTStressTests.Engine;

public interface IStressTestEngine
{
    Task<(List<RequestResult> Results, double ElapsedSeconds)> RunAsync(
        string method,
        string targetUrl,
        string requestBody,
        string? requestContentType,
        IReadOnlyDictionary<string, string> requestHeaders,
        int concurrency,
        int durationSeconds,
        CancellationToken externalCancellationToken = default);
}
```

### 3.5 统计与打印模块接口 (`JTStressTests.Statistics`)
```csharp
namespace JTStressTests.Statistics;

public interface IStatisticsCalculator
{
    double Average(IReadOnlyList<double> values);
    double Percentile(IReadOnlyList<double> sortedValues, double percentile);
    StressTestReport GenerateReport(IReadOnlyList<RequestResult> results, double elapsedSeconds);
}

/// <summary>
/// 报告输出策略接口 (策略模式)
/// </summary>
public interface IReportPrinter
{
    void Print(StressTestReport report);
}
```

### 3.6 导出模块接口 (`JTStressTests.Export`)
```csharp
namespace JTStressTests.Export;

/// <summary>
/// 压测结果导出策略接口 (策略模式)
/// </summary>
public interface IResultExporter
{
    string FileExtension { get; }
    string Generate(IEnumerable<RequestResult> results);
    void ExportToFile(string outputPath, IEnumerable<RequestResult> results);
}
```

---

## 4. 设计模式应用与 SOLID 原则落地

| 模块与类 | 应用的设计模式 | SOLID 原则体现 | 优势说明 |
| :--- | :--- | :--- | :--- |
| `IResultExporter` / `CsvResultExporter` | **策略模式 (Strategy Pattern)** | OCP (开闭原则), SRP (单一职责) | 后续扩展 JSON、Excel 或 Markdown 导出时，只需实现 `IResultExporter` 即可直接插拔，无需修改调用层逻辑。 |
| `IReportPrinter` / `ConsoleReportPrinter` | **策略模式 (Strategy Pattern)** | DIP (依赖倒置), SRP (单一职责) | 将报告格式化展示与计算分离，可轻松替换为文件输出、GUI 视图或 Webhook 策略。 |
| `IHttpClientFactory` / `SocketsHttpClientFactory` | **抽象工厂/工厂模式 (Factory Pattern)** | DIP (依赖倒置), SRP (单一职责) | 将底层的连接池生命周期、超时策略与压测引擎解耦，便于配置注入和针对极端高并发场景的独立参数调优。 |
| `IRequestBuilder` / `RequestBuilder` | **构建者/适配器模式 (Builder/Adapter)** | SRP (单一职责) | 将复杂的请求头映射、UTF-8 编码与 Content-Type 装配逻辑与并发调用调度解耦。 |
| `StressTestEngine` | **依赖倒置/组合** | DIP (依赖倒置) | 依赖 `IRequestBuilder` 接口，引擎本身不负责请求格式组装，支持单元测试通过 Mock 进行更细粒度验证。 |

---

## 5. 代码改动与差异说明 (Migration / Diff Guide)

1. **入口重构 (`Program.cs`)**：
   - 原先直接静态调用 `CommandLineParser.Parse`、`ConfigLoader.LoadConfig`、`StatisticsCalculator.PrintReport` 等静态类。
   - 重构后：通过各自的抽象接口（`ICommandLineParser`, `IConfigLoader`, `IHttpClientFactory`, `IRequestBuilder`, `IStatisticsCalculator`, `IReportPrinter`, `IResultExporter`）声明组件并协同工作，提升模块间松耦合度与扩展能力。
2. **目录独立迁移**：
   - 移除了单一集中放置的 `Models` 与 `Services` 强耦合聚合结构；
   - 建立了专属功能目录，每个目录均自包含接口、实现与专属领域实体。
3. **向后兼容机制 (`Services/*` & `Models/*`)**：
   - 为避免破坏外部可能存在的引用及原有单元测试，`JTStressTests.Services` 与 `JTStressTests.Models` 中提供了轻量级转接垫片与显/隐式转换操作符。
   - 现有代码如果仍然引用 `JTStressTests.Services.CommandLineParser.Parse` 等方法，将被透明地转发至新架构组件中执行，实现了 100% 平滑过渡与无破坏兼容。
4. **单测用例扩展**：
   - 新增 `RefactoredArchitectureTests.cs`，对新抽离出的接口、默认单例、策略模式、工厂实现及依赖注入组合进行了全面的行为断言，确保系统稳定性与健壮性。
