using System;
using System.Text.Json;

namespace Cosmos.BulkOperation.CLI;

/// <summary>
/// Custom PascalCase naming policy, because MS haven't implemented it yet: https://github.com/dotnet/runtime/issues/34114
/// </summary>
public sealed class JsonPascalCaseNamingPolicy : JsonNamingPolicy
{
    /// <summary>
    /// Converts the specified name to PascalCase.
    /// </summary>
    /// <param name="name">The name to convert.</param>
    /// <returns>The PascalCase version of the name.</returns>
    public override string ConvertName(string name)
    {
        if (string.IsNullOrEmpty(name) || !char.IsLower(name[0]))
        {
            return name;
        }

        var chars = name.ToCharArray();
        UppercaseFirstLetter(chars);
        return new string(chars);
    }

    private static void UppercaseFirstLetter(Span<char> chars)
    {
        chars[0] = char.ToUpperInvariant(chars[0]);
    }
}
