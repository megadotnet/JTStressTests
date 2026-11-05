# JTStressTests - High-Performance HTTP Load & Stress Testing Tool

[![.NET Version](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![Language](https://img.shields.io/badge/Language-C%23%2012-brightgreen.svg)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Unit Tests](https://img.shields.io/badge/Tests-81%20Passed-success.svg)](./TESTING.md)
[![CI/CD Pipeline](https://img.shields.io/badge/CI%2FCD-GitHub%20Actions-blue.svg)](./CI_WORKFLOW.md)

[English](./README.md) | [简体中文 (Chinese Version)](./ReadMe-ZhCn.md)

> **Notice & Disclaimer**: This tool is designed strictly for authorized performance, benchmark, and stress testing against target systems. Unauthorized stress testing or attacks against third-party systems are strictly prohibited.

---

## 1. Project Overview

`JTStressTests` is an ultra-lightweight, high-performance, asynchronous HTTP concurrency and stress testing CLI tool built on .NET 8 and C# 12. Designed with enterprise-grade resilience and low resource overhead, it allows software engineers and DevOps teams to benchmark target HTTP/HTTPS APIs under extreme concurrency.

### Core Highlights
- **High-Performance Connection Pooling**: Configured on top of low-level `SocketsHttpHandler` with socket reuse (`PooledConnectionLifetime` and `MaxConnectionsPerServer`), avoiding client-side socket starvation and port exhaustion under high loads.
- **Accurate Concurrency Control**: Uses `SemaphoreSlim` in coordination with the .NET Task thread pool to strictly constrain active in-flight requests without thread thrashing.
- **Graceful Termination & Signal Handling**: Captures OS-level termination signals (`Ctrl+C` / SIGINT) and timeout expirations to cleanly finalize in-flight requests with a graceful shutdown window (up to 10 seconds).
- **Comprehensive Latency & Error Metrics**: Computes precise latency percentiles (P50, P90, P95, P99) using continuous linear interpolation, aggregates HTTP status code distributions, and classifies error exception types.
- **Hierarchical Configuration Resolution**: Supports cascading configuration merging across `appsettings.json`, environment-specific files (`appsettings.{ASPNETCORE_ENVIRONMENT}.json`), and CLI arguments (CLI options take the highest precedence).
- **Data Export & Extensibility**: Exports per-request metrics (timestamps, response times, HTTP statuses, exception messages) to standard CSV format with CSV escaping. Built using the Strategy Pattern to allow seamless export format extensions (JSON, Parquet, Markdown).

---

## 2. Technology Stack Matrix

The project adopts a modern .NET ecosystem toolchain. Below is a detailed breakdown categorized into Frontend, Backend, Infrastructure, and Toolchain:

### 2.1 Frontend & User Interface
| Component / Framework | Specific Version | Core Role in Project |
| :--- | :--- | :--- |
| **CLI / Console UI Engine** | System Console (.NET 8.0) | Provides interactive terminal interface, ANSI-compatible text formatting, formatted metric tables, and live console progress feedback without heavy graphical dependencies. |
| **Command-Line Parser** | Custom Architecture (`JTStressTests.Cli`) | High-performance, zero-external-dependency argument parser supporting standard POSIX options, key-value syntax (`--flag value` and `--flag=value`), boolean flags, and multi-value parameters (e.g., custom HTTP headers). |

### 2.2 Backend & Runtime
| Component / Library | Specific Version | Core Role in Project |
| :--- | :--- | :--- |
| **Core Runtime Platform** | .NET Runtime 8.0 (`net8.0`) | Cross-platform modern managed execution environment providing native JIT optimization, GC efficiency, and Tiered PGO (Profile-Guided Optimization). |
| **Programming Language** | C# 12.0 (`LangVersion: latest`) | Primary programming language utilizing modern features: Top-Level Statements, Nullable Reference Types, Pattern Matching, Record Structs, and Primary Constructors. |
| **HTTP Transport Subsystem** | `System.Net.Http` (.NET 8.0 BCL) | Supplies high-throughput HTTP client infrastructure (`HttpClient`, `HttpRequestMessage`, `HttpResponseMessage`) with asynchronous streaming pipeline (`HttpCompletionOption.ResponseHeadersRead`). |
| **JSON Serialization Engine** | `System.Text.Json` (.NET 8.0 BCL) | High-performance, zero-allocation UTF-8 JSON parser and deserializer used for hierarchical configuration reading and cascading merge operations. |
| **Asynchronous Concurrency** | `System.Threading` (.NET 8.0 BCL) | Manages worker scheduling and throughput capping via `SemaphoreSlim`, `CancellationTokenSource`, and atomic operations (`Interlocked`). |
| **Lock-Free Data Collection** | `System.Collections.Concurrent` (.NET 8.0 BCL) | Provides lock-free, highly scalable metric record aggregation during multi-threaded stress execution via `ConcurrentBag<RequestResult>`. |
| **Architecture Abstractions** | Custom Enterprise Architecture | Decoupled domain interfaces implementing Strategy (`IResultExporter`, `IReportPrinter`), Factory (`IHttpClientFactory`), and Builder (`IRequestBuilder`) patterns. |

### 2.3 Infrastructure & Middleware
| Component / System | Specific Version | Core Role in Project |
| :--- | :--- | :--- |
| **HTTP Handler & Connection Pool** | `SocketsHttpHandler` (.NET 8.0) | Low-level OS socket handler with connection lifecycle management (`PooledConnectionLifetime = 5 min`, `MaxConnectionsPerServer = concurrency`, cookie container isolation, connection pooling). |
| **In-Memory Storage Pipeline** | Managed Memory (`ConcurrentBag<T>`) | In-memory zero-lock buffer collecting detailed per-request latency and status records, bypassing database bottlenecks during high-throughput benchmarking. |
| **OS Socket & TCP Stack** | POSIX / WinSock TCP/IP Stack | Underlying operating system socket networking layer managed via OS kernel configuration (`ulimit`, `tcp_tw_reuse`, ephemeral port ranges). |
| **Containerization Runtime** | Docker Engine >= 20.10 (OCI Compliant) | Standardized multi-stage container build and execution engine enabling isolated execution in cloud and CI/CD environments. |
| **Orchestration / Multi-Node** | Docker Compose >= 2.0 | Orchestration specification for multi-node distributed stress testing topologies. |

### 2.4 Toolchain & Testing
| Component / Tool | Specific Version | Core Role in Project |
| :--- | :--- | :--- |
| **Build System & Compiler** | .NET SDK 8.0 / MSBuild 17.8+ | Compiles C# source code, restores package dependencies, executes target project validation, and generates single-file self-contained production binaries. |
| **Unit Test Framework** | `MSTest` 4.0.1 | Microsoft's enterprise unit testing framework supporting test lifecycle attributes (`[TestClass]`, `[TestMethod]`), parameter-driven tests (`[DataRow]`), and assertion libraries. |
| **Mocking Framework** | `Moq` 4.21.0 | Generates mock delegates for `HttpMessageHandler` and domain abstractions to perform isolated offline testing of network failures, timeouts, and edge cases. |
| **Coverage Collector** | `coverlet.collector` 10.1.0 | Collects cross-platform code coverage metrics in standard Cobertura and OpenCover formats. |
| **Test Automation Script** | Bash Shell Script (`run-tests.sh`) | Unified shell automation script to execute the test suite and publish TRX and code coverage artifacts into `./TestResults`. |
| **Version Control** | Git 2.x+ | Source code version management and commit workflow. |

---

## 3. Environment & Compatibility Requirements

Ensure your target operating environment satisfies the minimum compatibility requirements:

| Dependency Component | Minimum Version | Recommended Version | Scope / Notes |
| :--- | :--- | :--- | :--- |
| **.NET SDK** | `8.0.100` | `8.0.x` or `10.0.x` LTS | Required for source code building, compiling, and running tests. |
| **.NET Runtime** | `8.0.0` | `8.0.x` | Required only if running framework-dependent published builds. |
| **Operating System** | | | Cross-platform binary compatibility: |
| - Linux | Kernel 3.10+ (glibc 2.23+ or musl 1.2+) | Ubuntu 20.04+, Debian 11+, RHEL 8+, Alpine 3.18+ | Primary production stress-testing environment. |
| - macOS | macOS 11.0 Big Sur (x64 / arm64) | macOS 13 Ventura or newer (Apple Silicon / Intel) | Supported for local development and benchmarking. |
| - Windows | Windows 10 (Build 1607+) / Windows Server 2016+ | Windows 11 / Windows Server 2022 (x64 / arm64) | Supported for native execution and Visual Studio development. |
| **Docker (Optional)** | `20.10.0` | `24.0.x`+ | Optional; required for containerized builds and execution. |
| **Docker Compose (Optional)** | `2.0.0` | `v2.20.0`+ | Optional; required for multi-node load execution. |

---

## 4. Local Deployment & Execution Guide

Follow these verified terminal commands to restore, compile, test, and run the project across Windows, macOS, and Linux.

### 4.1 Clone Repository & Build Project

```bash
# Clone the repository
git clone https://github.com/your-org/JTStressTests.git
cd JTStressTests

# Restore dependencies and build the solution in Debug mode
dotnet build
```

### 4.2 Run Automated Tests & Code Coverage

```bash
# Option A: Execute using the automated cross-platform test script (Linux / macOS / Git Bash)
chmod +x run-tests.sh
./run-tests.sh

# Option B: Run tests directly via dotnet CLI (All platforms: Windows PowerShell / macOS / Linux)
dotnet test

# Option C: Run tests with code coverage collection
dotnet test --collect:"XPlat Code Coverage"
```

### 4.3 Direct Run via .NET CLI

```bash
# Run with default configuration from appsettings.json
dotnet run

# Run with custom CLI arguments (Linux / macOS / PowerShell)
dotnet run -- --url "https://api.example.com/v1/health" --concurrency 100 --duration 30 --method GET

# Run POST request with payload and custom HTTP headers
dotnet run -- \
  --url "https://api.example.com/v1/login" \
  --concurrency 200 \
  --duration 60 \
  --method POST \
  --body '{"username":"tester","token":"xyz"}' \
  --header "Authorization: Bearer token123" \
  --header "X-Trace-Id: trace-9999" \
  --output result.csv
```

### 4.4 Publish Self-Contained Single-File Executables

For maximum throughput without installing the .NET runtime on target load testing servers, publish a self-contained, single-file binary:

#### Linux (x64 / ARM64)
```bash
# Linux x64
dotnet publish JTStressTests.csproj -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true -o ./publish-linux
cd ./publish-linux
chmod +x ./JTStressTests
./JTStressTests --url "https://api.example.com/api" --concurrency 500 --duration 60 --output report.csv
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
# Windows x64
dotnet publish JTStressTests.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o .\publish-win
cd .\publish-win
.\JTStressTests.exe --url "https://api.example.com/api" --concurrency 300 --duration 45 --output report.csv
```

### 4.5 Containerized Deployment (Docker & Docker Compose)

#### Using Docker
```bash
# 1. Build the multi-stage lightweight production Docker container image
docker build -t jt-stress-tests:1.0.0 -t jt-stress-tests:latest .

# 2. Run in Cloud-Native Web Daemon mode (Non-root user 10001, port 8080)
docker run -d --name jt-stress-service -p 8080:8080 jt-stress-tests:latest

# Health check inspection
curl http://127.0.0.1:8080/healthz

# 3. Or run containerized benchmark CLI with output mounted to local host directory
docker run --rm \
  -v "$(pwd)/output:/app/output" \
  jt-stress-tests:latest \
  --url "https://api.example.com/test" \
  --concurrency 250 \
  --duration 60 \
  --output /app/output/benchmark.csv
```

#### Using Docker Compose (Local Development & Stack)
```bash
# Launch web daemon service alongside mock-target service
docker compose up -d

# Verify health status
docker compose ps
curl http://127.0.0.1:8080/healthz

# Tear down environment
docker compose down
```

#### Cloud-Native Kubernetes Deployment
All production Kubernetes resources are provided under `k8s/`:
- `deployment.yaml`: Enforces non-root execution (`runAsUser: 10001`), read-only root filesystem (`readOnlyRootFilesystem: true`), drops all Linux capabilities, and declares liveness, readiness, and startup probes.
- `service.yaml`: ClusterIP service routing traffic to port 8080.
- `ingress.yaml`: NGINX Ingress resource supporting TLS termination.
- `hpa.yaml`: Horizontal Pod Autoscaler targeting CPU (75%) and Memory (80%) utilization.
- `pvc.yaml`: Persistent volume claim for benchmark report persistence.
- `configmap.yaml` & `secret.yaml`: Strict configuration and credential segregation.

Deploy using Kustomize:
```bash
kubectl apply -k k8s/
```

---

## 5. Command-Line Options Reference

All CLI parameters can be provided via space notation (`--flag value`) or equals notation (`--flag=value`). CLI arguments take precedence over any settings in `appsettings.json`.

| Parameter | Type | Default Value | Description |
| :--- | :--- | :--- | :--- |
| `--url` | string | Read from `appsettings.json` | Target API endpoint URL (HTTP or HTTPS). |
| `--concurrency` | int | `500` | Number of concurrent worker tasks (must be a positive integer). |
| `--duration` | int | `60` | Duration of the benchmark test in seconds (must be a positive integer). |
| `--method` | string | `POST` | HTTP request method (e.g., `GET`, `POST`, `PUT`, `DELETE`, `PATCH`). |
| `--body` | string | Read from `appsettings.json` | Request payload body string. |
| `--header` | string | Read from `appsettings.json` | Custom HTTP header in `Name: Value` format. Can be repeated multiple times. |
| `--output` | string | Empty (disabled) | Destination file path to export request records as CSV. |
| `--server` | flag | `false` | Runs the application in Cloud-Native Web Daemon mode serving health checks and REST endpoints. |
| `--port` | int | `8080` | Port to bind for the Web Daemon server (can also be set via `PORT` environment variable). |
| `--help`, `-h` | flag | `false` | Displays command usage and available options. |

---

## 6. Project Architecture & Directory Structure

The project is structured according to SOLID principles, decoupling concerns into domain-specific modules with legacy facade compatibility:

```text
.
├── JTStressTests.sln                  # Visual Studio / .NET solution file
├── JTStressTests.csproj               # Main executable project file (.NET 8.0)
├── Program.cs                         # Application entry point (Top-level statements & DI orchestration)
├── appsettings.json                   # Base configuration file (Default endpoints and headers)
├── run-tests.sh                       # Automated test & code coverage execution script
├── README.md                          # Standard English documentation (Current file)
├── ReadMe-ZhCn.md                     # Standard Simplified Chinese documentation
├── CI_WORKFLOW.md                     # CI/CD workflow specification, management rules & build paths
├── TESTING.md                         # Unit testing specifications & coverage report guide
├── REFACTORING.md                     # Architecture refactoring, interface contracts & design patterns
│
├── Cli/                               # Command-Line Domain Module
│   ├── ICommandLineParser.cs          # Command-line parsing abstraction
│   ├── CommandLineParser.cs           # Robust token-based argument parser implementation
│   └── CommandLineOptions.cs          # Domain data model for CLI arguments
│
├── Configuration/                     # Configuration Management Domain Module
│   ├── IConfigLoader.cs               # Configuration loader & cascading merge abstraction
│   ├── ConfigLoader.cs                # Multi-environment JSON deserializer & merge implementation
│   └── AppConfig.cs                   # Configuration POCO model
│
├── Http/                              # Network Transport & Construction Module
│   ├── IRequestBuilder.cs             # Request builder abstraction (Builder Pattern)
│   ├── RequestBuilder.cs              # HttpRequestMessage builder implementation
│   ├── IHttpClientFactory.cs          # HttpClient factory abstraction (Factory Pattern)
│   └── SocketsHttpClientFactory.cs    # Factory configuring SocketsHttpHandler connection pooling
│
├── Engine/                            # Concurrency Stress Execution Engine
│   ├── IStressTestEngine.cs           # Stress test runner engine abstraction
│   ├── StressTestEngine.cs            # High-throughput runner utilizing SemaphoreSlim
│   └── RequestResult.cs               # Per-request execution result data record
│
├── Statistics/                        # Performance Metrics & Reporting Module
│   ├── IStatisticsCalculator.cs       # Statistical metric calculator abstraction
│   ├── StatisticsCalculator.cs        # Linear interpolation percentile & aggregation logic
│   ├── IReportPrinter.cs              # Report printer strategy abstraction (Strategy Pattern)
│   ├── ConsoleReportPrinter.cs        # ANSI console formatted table printer implementation
│   └── StressTestReport.cs            # Aggregated performance metric report model
│
├── Export/                            # Metric Persistence & Export Module
│   ├── IResultExporter.cs             # Persistence exporter strategy interface (Strategy Pattern)
│   └── CsvResultExporter.cs           # RFC-compliant CSV exporter with escaping
│
├── Services/                          # [Backward Compatibility Facade Layer]
│   ├── CommandLineParser.cs           # Static facade forwarding to Cli domain
│   ├── ConfigLoader.cs                # Static facade forwarding to Configuration domain
│   ├── RequestBuilder.cs              # Static facade forwarding to Http domain
│   ├── StressTestEngine.cs            # Facade forwarding to Engine domain
│   ├── StatisticsCalculator.cs        # Static facade forwarding to Statistics domain
│   └── CsvExporter.cs                 # Static facade forwarding to Export domain
│
├── Models/                            # [Backward Compatibility Model Layer]
│   ├── AppConfig.cs                   # Type aliasing & implicit operators for AppConfig
│   ├── CommandLineOptions.cs          # Type aliasing & implicit operators for CommandLineOptions
│   ├── RequestResult.cs               # Type aliasing & implicit operators for RequestResult
│   └── StressTestReport.cs            # Type aliasing & implicit operators for StressTestReport
│
└── JTStressTests.Tests/               # Unit Test Suite Project (.NET 8.0)
    ├── JTStressTests.Tests.csproj     # Test project file (MSTest 4.0.1, Moq 4.21.0, Coverlet 10.1.0)
    ├── CommandLineParserTests.cs      # CLI parsing edge cases, defaults, errors & flag validation
    ├── ConfigLoaderTests.cs           # JSON serialization, invalid files, cascading overrides
    ├── RequestBuilderTests.cs         # Method handling, headers, payload encoding & content types
    ├── StatisticsCalculatorTests.cs   # Linear interpolation percentiles, aggregations, QPS
    ├── CsvExporterTests.cs            # Escaping special characters, sorting, file generation
    ├── StressTestEngineTests.cs       # Mock HttpMessageHandler concurrency, cancellation, errors
    ├── ModelTests.cs                  # Model immutability, properties & state validation
    └── RefactoredArchitectureTests.cs # Strategy, Factory, Builder patterns & DI integration
```

---

## 7. Development Standards & Conventions

To maintain high code quality and software reliability, all contributions must adhere to the following development conventions:

### 7.1 Coding Conventions & Design Principles
- **Object-Oriented SOLID Design**:
  - **Single Responsibility (SRP)**: Each class has one specific responsibility (e.g., parsing CLI options, calculating statistics, exporting CSV).
  - **Open/Closed (OCP)**: Interfaces such as `IResultExporter` and `IReportPrinter` allow introducing new export formats (e.g., JSON, Prometheus) without changing caller code.
  - **Dependency Inversion (DIP)**: `Program.cs` and `StressTestEngine` depend on abstractions (`IRequestBuilder`, `IHttpClientFactory`), facilitating unit testing through test doubles and mocks.
- **C# Language Standards**:
  - Enabled `<Nullable>enable</Nullable>`: All reference types must explicitly handle nullability.
  - Enabled `<ImplicitUsings>enable</ImplicitUsings>`: Standard system namespaces are globally imported.
  - Avoid blocking calls (`.Result`, `.Wait()`). All I/O operations must be fully asynchronous (`async` / `await`).
  - Use `HttpCompletionOption.ResponseHeadersRead` combined with `CopyToAsync(Stream.Null)` to release network buffers promptly.

### 7.2 Testing & Quality Assurance Standards
- **Triple-A Pattern (AAA)**: Every test case must follow `Arrange`, `Act`, and `Assert`.
- **Naming Convention**: Test methods must follow `MethodUnderTest_StateUnderTest_ExpectedBehavior` (e.g., `Percentile_EvenNumberOfElements_CalculatesLinearInterpolation`).
- **Isolation via Test Doubles**: Network dependencies must be simulated using `Moq` against `HttpMessageHandler` to ensure deterministic execution without external network calls.
- **Coverage Standard**: Core domain modules must maintain a minimum of **95%+** branch and statement coverage.

---

## 8. Troubleshooting & FAQ

### 8.1 Socket Starvation or `SocketException` under High Concurrency
- **Symptom**: `SocketException: Address already in use` or connection timeout during intense benchmarks (> 1000 concurrency).
- **Cause**: Client OS running out of available ephemeral ports or hitting file descriptor limits.
- **Resolution**:
  1. Increase the Linux file descriptor limit:
     ```bash
     ulimit -n 65535
     ```
  2. Optimize TCP port recycling and reuse in `/etc/sysctl.conf`:
     ```bash
     sudo sysctl -w net.ipv4.ip_local_port_range="1024 65535"
     sudo sysctl -w net.ipv4.tcp_tw_reuse=1
     ```

### 8.2 Memory Growth During Prolonged Benchmarking
- **Symptom**: High memory usage when running long benchmarks (e.g., duration > 1 hour at 20,000+ QPS).
- **Cause**: Per-request metrics are recorded in memory in `ConcurrentBag<RequestResult>` to compute exact percentile distributions. Millions of requests accumulate significant memory overhead.
- **Resolution**:
  - Run benchmark tests in discrete batches (e.g., 60 to 300 seconds per batch).
  - Provide a reasonable `--duration` flag.

### 8.3 DNS Changes Not Reflected
- **Symptom**: Target domain IP changed during stress testing, but client continues hitting old server instances.
- **Cause**: Connection pooling keeps persistent TCP sockets open.
- **Resolution**:
  - `SocketsHttpClientFactory` sets `PooledConnectionLifetime = TimeSpan.FromMinutes(5)`, ensuring TCP connections are gracefully recreated every 5 minutes to trigger fresh DNS queries.

### 8.4 Configuration Precedence Confusion
- **Symptom**: Changes made in `appsettings.json` do not take effect when passing CLI options.
- **Resolution**:
  - Configuration cascading follows a strict order of priority:
    $$\text{CLI Arguments} > \text{appsettings.\{ASPNETCORE\_ENVIRONMENT\}.json} > \text{appsettings.json}$$
  - Inspect terminal output to verify resolved effective settings.

---

## 9. License

This project is licensed under the [MIT License](LICENSE).
