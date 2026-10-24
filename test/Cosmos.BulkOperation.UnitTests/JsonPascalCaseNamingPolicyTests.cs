using System.Text.Json;

using Cosmos.BulkOperation.CLI;

using Shouldly;

namespace Cosmos.BulkOperation.UnitTests;

/// <summary>
/// Tests for the <see cref="JsonPascalCaseNamingPolicy"/>.
/// </summary>
public class JsonPascalCaseNamingPolicyTests
{
    /// <summary>
    /// Verifies that names starting with a lowercase letter are converted to PascalCase.
    /// </summary>
    [Theory(DisplayName = "Converts lowercase names to PascalCase")]
    [InlineData("lowercase", "Lowercase")]
    [InlineData("exampleName", "ExampleName")]
    [InlineData("u", "U")]
    public void ConvertName_WithLowercaseName_ConvertsToPascalCase(string name, string expected)
        => new JsonPascalCaseNamingPolicy().ConvertName(name).ShouldBe(expected);

    /// <summary>
    /// Verifies that names not starting with a lowercase letter are left unchanged.
    /// </summary>
    [Theory(DisplayName = "Leaves names unchanged when they do not start with a lowercase letter")]
    [InlineData("PascalCase", "PascalCase")]
    [InlineData("UPPERCASE", "UPPERCASE")]
    [InlineData("_underscore", "_underscore")]
    [InlineData("9number", "9number")]
    [InlineData("", "")]
    public void ConvertName_WithNonLowercaseName_ReturnsNameUnchanged(string name, string expected)
        => new JsonPascalCaseNamingPolicy().ConvertName(name).ShouldBe(expected);

    /// <summary>
    /// Verifies that a null name is returned unchanged.
    /// </summary>
    [Fact(DisplayName = "Returns null unchanged")]
    public void ConvertName_WithNullName_ReturnsNull()
        => new JsonPascalCaseNamingPolicy().ConvertName(null).ShouldBeNull();
}
