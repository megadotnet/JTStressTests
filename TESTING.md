# 单元测试与测试覆盖率文档 (Unit Testing Guide)

本项目根据行业标准规范 (`unittesting.md`) 搭建了完整的单元测试工程体系，确保核心业务模块、命令行解析、配置加载、HTTP 请求构建、压测引擎与数据统计/导出的代码质量和鲁棒性。

---

## 1. 测试工程架构与技术栈

测试工程位于 `JTStressTests.Tests/`，主要技术组件包括：
- **测试框架**：`MSTest` 4.0.1（原生支持 `[TestClass]`, `[TestMethod]`, `[DataRow]` 参数化测试及精准断言库）
- **Mock 依赖注入与模拟**：`Moq` 4.21.0（模拟 `HttpMessageHandler` 与 HTTP 客户端请求响应链路，摆脱外部真实网络依赖）
- **代码覆盖率收集工具**：`coverlet.collector` 10.1.0（支持 Cobertura 与 OpenCover 覆盖率格式）
- **目标运行时**：.NET 8.0 (`net8.0`)，与主项目 `JTStressTests` 技术栈 100% 保持一致

---

## 2. 测试用例矩阵与覆盖范围

所有单元测试用例均遵循 **Arrange-Act-Assert (AAA)** 模式与 `Method_Condition_ExpectedResult` 命名约定，覆盖正向流程、边界极值与异常错误分支。

| 测试类 (Test Class) | 覆盖核心模块 | 覆盖的关键测试场景 |
| :--- | :--- | :--- |
| **`CommandLineParserTests`** | 命令行 CLI 解析 (`CommandLineParser`) | - `--help` 与 `-h` 帮助标志解析<br>- 空参数默认值解析 (concurrency=500, duration=60, method=POST)<br>- 空格分隔 (`--flag value`) 与等号分隔 (`--flag=value`) 混合解析<br>- 参数前缀异常 (`-flag`, `flag`, 未知参数)<br>- 参数值缺失 (`--concurrency` 等缺少后续值)<br>- 非法数字 (并发数/时长为 0、负数、非数字字符串)<br>- 畸形 Header 格式 (无冒号、冒号位于首位)<br>- `PrintUsage` 帮助输出完整性 |
| **`ConfigLoaderTests`** | 配置加载与层级合并 (`ConfigLoader`) | - 正常 JSON 配置反序列化<br>- 畸形 JSON 语法抛出 `JsonException`<br>- 缺少配置文件返回 null<br>- 基础配置加载与多环境 `appsettings.{env}.json` 级联覆盖合并<br>- 命令行 CLI 参数对配置文件的最高优先级覆盖<br>- Content-Type 优先级解析与 Header 映射 |
| **`RequestBuilderTests`** | 请求构建引擎 (`RequestBuilder`) | - 支持所有标准 HTTP 方法 (GET, POST, PUT, DELETE)<br>- URL 与 UTF-8 请求体装配<br>- Content-Type 正确注入 `HttpContent` 头<br>- 自定义请求头 (如 Authorization, X-Request-Id) 注入与防冲突<br>- Content-Type 缺省时默认回退行为 |
| **`StatisticsCalculatorTests`** | 性能统计与百分位计算 (`StatisticsCalculator`) | - 平均耗时计算 (含空集合返回 0)<br>- 延迟百分位数计算 (P50, P90, P95, P99) 基于线性插值法算法的严格单测 (含空列表、单项列表、多项线性插值边界)<br>- QPS 与成功率计算<br>- HTTP 状态码分布与异常类型按前缀聚合<br>- `PrintReport` 控制台控制格式输出无报错 |
| **`CsvExporterTests`** | CSV 数据导出与转义 (`CsvExporter`) | - CSV 字段特殊字符转义 (`EscapeCsv` 对逗号、双引号、换行符 `\n`、`\r\n` 转义)<br>- 空结果输出标准 CSV 标题行<br>- 结果按 `Index` 升序排序与时间戳 ISO 格式化<br>- 导出结果文件写入磁盘与校验 |
| **`StressTestEngineTests`** | 并发压测执行引擎 (`StressTestEngine`) | - 构造参数非空契约校验<br>- 默认 HttpClient 实例生成与超时配置<br>- 基于 Mock `HttpMessageHandler` 模拟并发请求接收、状态码记录与响应流丢弃 (`CopyToAsync(Stream.Null)`)<br>- 混合异常模拟 (HTTP 500、网络 `HttpRequestException` 等) 结果采集与计数<br>- 提前触发 `CancellationToken` 平滑优雅退出与无死锁保障 |
| **`ModelTests`** | 数据传输模型 (`Models`) | - `RequestResult` 构造与只读属性验证<br>- `AppConfig`, `CommandLineOptions`, `StressTestReport` 属性赋值与序列化完整性 |
| **`RefactoredArchitectureTests`** | 重构抽象接口与设计模式实现 | - `ICommandLineParser`, `IConfigLoader` 接口契约与合并逻辑<br>- `SocketsHttpClientFactory` 工厂实现与客户端配置<br>- `CsvResultExporter` 与 `HtmlResultExporter` 策略实现与标准输出格式<br>- `ConsoleReportPrinter` 报告输出策略<br>- `StressTestEngine` 依赖注入 `IRequestBuilder` 协作验证 |
| **`HtmlExporterTests`** | HTML 可视化报告导出与图表呈现验证 | - `HtmlResultExporter` 契约与文件写入<br>- 耗时分位数柱状图、状态码环形图、时间序列折线与分布直方图渲染<br>- XSS 安全防范与特殊字符转义<br>- `--html` 命令行参数与向后兼容门面 |

---

## 3. 运行测试与生成覆盖率报告

### 3.1 一键执行脚本 (推荐)

项目根目录提供了跨平台一键测试脚本 `run-tests.sh`：

```bash
# 赋予执行权限并运行
chmod +x run-tests.sh
./run-tests.sh
```

脚本将自动执行所有单元测试并收集代码覆盖率，在 `./TestResults` 目录下输出：
- `test_results.trx`：Visual Studio / CI 标准测试结果报告
- `coverage.cobertura.xml`：标准 Cobertura 格式代码覆盖率文件
- `coverage.opencover.xml`：OpenCover 格式代码覆盖率文件

### 3.2 通过 dotnet 命令直接运行

```bash
# 运行全部单测
dotnet test

# 运行单测并收集覆盖率
dotnet test --collect:"XPlat Code Coverage"

# 按测试类筛选运行
dotnet test --filter "FullyQualifiedName~CommandLineParser"
```

---

## 4. 覆盖率指标说明

执行测试后，核心业务逻辑模块（`JTStressTests.Services.*` 与 `JTStressTests.Models.*`）的代码行覆盖率与分支覆盖率均达到 **95%+**（除独立入口外所有核心模块均为 95%~100% 全覆盖），全部 68 项单测用例 100% 通过。
