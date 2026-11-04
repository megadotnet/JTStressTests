using System.Text.Json;
using JTStressTests.Models;
using JTStressTests.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JTStressTests.Tests;

[TestClass]
public sealed class ConfigLoaderTests
{
    private string _tempDirectory = null!;

    [TestInitialize]
    public void SetUp()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "JTStressTests_Test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [TestCleanup]
    public void TearDown()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

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

    [TestMethod]
    public void LoadConfig_MalformedJson_ThrowsJsonException()
    {
        // Arrange
        string filePath = Path.Combine(_tempDirectory, "bad.json");
        File.WriteAllText(filePath, "{ invalid_json ");

        // Act & Assert
        Assert.Throws<JsonException>(() => ConfigLoader.LoadConfig(filePath));
    }

    [TestMethod]
    public void LoadMergedConfig_BaseFileDoesNotExist_ReturnsNull()
    {
        // Act
        var config = ConfigLoader.LoadMergedConfig(_tempDirectory);

        // Assert
        Assert.IsNull(config);
    }

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
        Assert.AreEqual("https://staging.org", config.Url); // Overridden
        Assert.AreEqual("base_body", config.Body);           // Preserved from base
        Assert.AreEqual("text/plain", config.ContentType);   // Preserved from base
        Assert.IsNotNull(config.Headers);
        Assert.AreEqual("Base1", config.Headers["H1"]);
        Assert.AreEqual("Staging2", config.Headers["H2"]);  // Overridden
        Assert.AreEqual("Staging3", config.Headers["H3"]);  // Added
    }

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
