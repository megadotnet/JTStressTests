// ============================================================================
// 文件名称: ConfigLoaderTests.cs
// 核心功能: 配置加载器 ConfigLoader 单元测试套件，验证 JSON 反序列化、多环境覆盖及命令行参数优先级合并
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: System.Text.Json, Microsoft.VisualStudio.TestTools.UnitTesting, JTStressTests.Models, JTStressTests.Services
// ============================================================================

using System.Text.Json;
using JTStressTests.Models;
using JTStressTests.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JTStressTests.Tests;

/// <summary>
/// 配置加载器功能与规则单元测试类
/// </summary>
[TestClass]
public sealed class ConfigLoaderTests
{
    /// <summary>
    /// 测试专用的隔离临时目录路径
    /// </summary>
    private string _tempDirectory = null!;

    /// <summary>
    /// 每个测试用例执行前的初始化方法，创建隔离的临时测试目录
    /// </summary>
    [TestInitialize]
    public void SetUp()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "JTStressTests_Test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    /// <summary>
    /// 每个测试用例执行完毕后的清理方法，递归删除测试临时目录
    /// </summary>
    [TestCleanup]
    public void TearDown()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    /// <summary>
    /// 验证合法的 JSON 配置文件能被正确加载并反序列化至 AppConfig 对象
    /// </summary>
    [TestMethod]
    public void LoadConfig_ValidJsonFile_DeserializesCorrectly()
    {
        // Arrange
        string filePath = Path.Combine(_tempDirectory, "test-config.json");
        string json = """
        {
            "Url": "https://api.test.com/run",
            "Body": "test_body",
            "ContentType": "application/json",
            "Headers": {
                "User-Agent": "TestClient/1.0",
                "X-Api-Key": "secret123"
            }
        }
        """;
        File.WriteAllText(filePath, json);

        // Act
        var config = ConfigLoader.LoadConfig(filePath);

        // Assert
        Assert.IsNotNull(config);
        Assert.AreEqual("https://api.test.com/run", config.Url);
        Assert.AreEqual("test_body", config.Body);
        Assert.AreEqual("application/json", config.ContentType);
        Assert.IsNotNull(config.Headers);
        Assert.AreEqual("TestClient/1.0", config.Headers["User-Agent"]);
        Assert.AreEqual("secret123", config.Headers["X-Api-Key"]);
    }

    /// <summary>
    /// 验证当 JSON 文件格式存在语法错误时正确抛出 JsonException
    /// </summary>
    [TestMethod]
    public void LoadConfig_MalformedJson_ThrowsJsonException()
    {
        // Arrange
        string filePath = Path.Combine(_tempDirectory, "bad.json");
        File.WriteAllText(filePath, "{ invalid_json ");

        // Act & Assert
        Assert.Throws<JsonException>(() => ConfigLoader.LoadConfig(filePath));
    }

    /// <summary>
    /// 验证当基础配置文件 appsettings.json 不存在时 LoadMergedConfig 返回 null
    /// </summary>
    [TestMethod]
    public void LoadMergedConfig_BaseFileDoesNotExist_ReturnsNull()
    {
        // Act
        var config = ConfigLoader.LoadMergedConfig(_tempDirectory);

        // Assert
        Assert.IsNull(config);
    }

    /// <summary>
    /// 验证仅存在基础配置文件时的正确加载
    /// </summary>
    [TestMethod]
    public void LoadMergedConfig_BaseConfigOnly_LoadsSuccessfully()
    {
        // Arrange
        string basePath = Path.Combine(_tempDirectory, "appsettings.json");
        File.WriteAllText(basePath, """{"Url": "https://base.org", "ContentType": "text/plain"}""");

        // Act
        var config = ConfigLoader.LoadMergedConfig(_tempDirectory, environment: string.Empty);

        // Assert
        Assert.IsNotNull(config);
        Assert.AreEqual("https://base.org", config.Url);
        Assert.AreEqual("text/plain", config.ContentType);
    }

    /// <summary>
    /// 验证多环境配置文件存在时正确执行属性级联覆盖与字典合并
    /// </summary>
    [TestMethod]
    public void LoadMergedConfig_WithEnvironmentFile_MergesAndOverridesBase()
    {
        // Arrange
        string basePath = Path.Combine(_tempDirectory, "appsettings.json");
        File.WriteAllText(basePath, """
        {
            "Url": "https://base.org",
            "Body": "base_body",
            "ContentType": "text/plain",
            "Headers": {
                "H1": "Base1",
                "H2": "Base2"
            }
        }
        """);

        string envPath = Path.Combine(_tempDirectory, "appsettings.Staging.json");
        File.WriteAllText(envPath, """
        {
            "Url": "https://staging.org",
            "Headers": {
                "H2": "Staging2",
                "H3": "Staging3"
            }
        }
        """);

        // Act
        var config = ConfigLoader.LoadMergedConfig(_tempDirectory, environment: "Staging");

        // Assert
        Assert.IsNotNull(config);
        Assert.AreEqual("https://staging.org", config.Url); // 被环境配置覆盖
        Assert.AreEqual("base_body", config.Body);           // 继承基础配置
        Assert.AreEqual("text/plain", config.ContentType);   // 继承基础配置
        Assert.IsNotNull(config.Headers);
        Assert.AreEqual("Base1", config.Headers["H1"]);
        Assert.AreEqual("Staging2", config.Headers["H2"]);  // 覆盖请求头
        Assert.AreEqual("Staging3", config.Headers["H3"]);  // 新增请求头
    }

    /// <summary>
    /// 验证基准配置为 null 时 Merge 方法正确直接应用覆盖配置的属性
    /// </summary>
    [TestMethod]
    public void Merge_NullBaseConfig_ReturnsOverrideConfigValues()
    {
        // Arrange
        var overrideConfig = new AppConfig
        {
            Url = "https://new.org",
            Body = "new_body",
            ContentType = "application/json",
            Headers = new Dictionary<string, string> { ["A"] = "1" }
        };

        // Act
        var merged = ConfigLoader.Merge(null, overrideConfig);

        // Assert
        Assert.AreEqual("https://new.org", merged.Url);
        Assert.AreEqual("new_body", merged.Body);
        Assert.AreEqual("application/json", merged.ContentType);
        Assert.AreEqual("1", merged.Headers!["A"]);
    }

    /// <summary>
    /// 验证命令行传入的参数优先于配置文件中的同名项
    /// </summary>
    [TestMethod]
    public void ResolveEffectiveConfig_CliOverridesConfigAndDefaultContentType()
    {
        // Arrange
        var config = new AppConfig
        {
            Url = "https://config.org",
            Body = "config_body",
            ContentType = "application/xml",
            Headers = new Dictionary<string, string>
            {
                ["Header1"] = "Val1",
                ["Header2"] = "Val2"
            }
        };

        var cliOptions = new CommandLineOptions
        {
            Url = "https://cli.org",
            Body = "cli_body",
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Header2"] = "CliVal2",
                ["Content-Type"] = "application/json"
            }
        };

        // Act
        var (url, body, contentType, headers) = ConfigLoader.ResolveEffectiveConfig(config, cliOptions);

        // Assert
        Assert.AreEqual("https://cli.org", url);
        Assert.AreEqual("cli_body", body);
        Assert.AreEqual("application/json", contentType);
        Assert.AreEqual("Val1", headers["Header1"]);
        Assert.AreEqual("CliVal2", headers["Header2"]);
    }

    /// <summary>
    /// 验证在未指定命令行参数时降级回退至配置文件对应的默认值
    /// </summary>
    [TestMethod]
    public void ResolveEffectiveConfig_MissingCliValues_FallsBackToConfigValues()
    {
        // Arrange
        var config = new AppConfig
        {
            Url = "https://config.org",
            Body = "config_body",
            ContentType = "text/plain"
        };
        var cliOptions = new CommandLineOptions();

        // Act
        var (url, body, contentType, headers) = ConfigLoader.ResolveEffectiveConfig(config, cliOptions);

        // Assert
        Assert.AreEqual("https://config.org", url);
        Assert.AreEqual("config_body", body);
        Assert.AreEqual("text/plain", contentType);
        Assert.IsEmpty(headers);
    }
}
