using MsbuildAnalyzer.Models;
using MsbuildAnalyzer.Utilities;
using Xunit;

namespace MsbuildAnalyzer.Tests;

/// <summary>
/// What makes a project a test project, and so which projects get a `test`
/// target instead of `publish`/`run` or `pack`.
/// </summary>
public class TestProjectDetectionTests
{
    private static bool IsTestProject(
        Dictionary<string, string>? properties = null,
        params string[] packages)
    {
        return ProjectUtilities.IsTestProject(
            new Dictionary<string, string>(
                properties ?? [],
                StringComparer.OrdinalIgnoreCase),
            [.. packages.Select(p => new PackageReference { Include = p })]);
    }

    private static Dictionary<string, string> Property(string name, string value) =>
        new() { [name] = value };

    [Fact]
    public void NoSignals_IsNotATestProject()
    {
        Assert.False(IsTestProject());
    }

    [Fact]
    public void OrdinaryLibrary_IsNotATestProject()
    {
        Assert.False(IsTestProject(
            Property("IsTestProject", "false"),
            "Newtonsoft.Json",
            "Serilog"));
    }

    [Theory]
    [InlineData("IsTestProject")]
    // Microsoft.Testing.Platform sets this instead of IsTestProject, and the
    // xunit.v3 and TUnit runners reach Nx through it. NXC-4963.
    [InlineData("IsTestingPlatformApplication")]
    public void TestProperty_IsATestProject(string property)
    {
        Assert.True(IsTestProject(Property(property, "true")));
    }

    // MSBuild compares booleans case-insensitively, so a project that spells the
    // property `True` is a test project as far as MSBuild is concerned.
    [Theory]
    [InlineData("IsTestProject", "True")]
    [InlineData("IsTestProject", "TRUE")]
    [InlineData("IsTestingPlatformApplication", "True")]
    public void TestProperty_IsCaseInsensitive(string property, string value)
    {
        Assert.True(IsTestProject(Property(property, value)));
    }

    [Theory]
    [InlineData("IsTestProject")]
    [InlineData("IsTestingPlatformApplication")]
    public void TestPropertySetFalse_IsNotATestProject(string property)
    {
        Assert.False(IsTestProject(Property(property, "false")));
    }

    [Theory]
    [InlineData("Microsoft.NET.Test.Sdk")]
    [InlineData("Microsoft.Testing.Platform")]
    [InlineData("Microsoft.Testing.Extensions.CodeCoverage")]
    [InlineData("xunit")]
    [InlineData("xunit.v3")]
    // The reference OrchardCore's test projects actually carry. NXC-4963.
    [InlineData("xunit.v3.mtp-v2")]
    [InlineData("xunit.runner.visualstudio")]
    [InlineData("NUnit")]
    [InlineData("NUnit3TestAdapter")]
    [InlineData("MSTest")]
    [InlineData("MSTest.TestFramework")]
    [InlineData("TUnit")]
    public void TestPackage_IsATestProject(string package)
    {
        Assert.True(IsTestProject(packages: package));
    }

    // NuGet package ids are case-insensitive, so the casing a project happens to
    // write a reference in cannot decide whether it gets a test target.
    [Theory]
    [InlineData("XUnit.v3")]
    [InlineData("nunit")]
    [InlineData("microsoft.net.test.sdk")]
    public void TestPackage_IsCaseInsensitive(string package)
    {
        Assert.True(IsTestProject(packages: package));
    }

    [Fact]
    public void TestPackageAmongOthers_IsATestProject()
    {
        Assert.True(IsTestProject(
            properties: null,
            "Newtonsoft.Json",
            "xunit.v3.mtp-v2",
            "Moq"));
    }
}
