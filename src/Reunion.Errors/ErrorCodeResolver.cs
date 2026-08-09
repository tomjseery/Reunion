using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Reunion.Errors;

internal static partial class ErrorCodeResolver
{
    private static readonly ConcurrentDictionary<(Type Error, Type Case), string> Cache = new();

    internal static string Resolve(Type errorType, Type caseType) =>
        Cache.GetOrAdd((errorType, caseType), static types => ResolveUncached(types.Error, types.Case));

    private static string ResolveUncached(Type errorType, Type caseType)
    {
        var errorWords = SplitWords(RemoveErrorSuffix(errorType.Name, errorType), errorType);
        var caseWords = SplitWords(RemoveCaseSuffix(caseType.Name), caseType);

        return caseType.GetCustomAttribute<ErrorCodeAttribute>(inherit: false)?.Code
            ?? Derive(caseType, errorWords, caseWords);
    }

    private static string Derive(
        Type caseType,
        IReadOnlyList<string> errorWords,
        IReadOnlyList<string> caseWords)
    {
        var suffixWords = errorWords
            .Skip(1)
            .Concat(WithoutRepeatedContext(caseWords, errorWords))
            .ToArray();

        if (suffixWords.Length == 0)
        {
            throw new InvalidOperationException(
                $"{caseType.Name} only repeats its error owner's name and leaves no code suffix; "
                + "rename it or declare [ErrorCode].");
        }

        return $"{errorWords[0].ToLowerInvariant()}.{ToSnakeCase(suffixWords)}";
    }

    private static string RemoveErrorSuffix(string errorName, Type errorType)
    {
        if (!errorName.EndsWith("Error", StringComparison.Ordinal)
            || errorName.Length == "Error".Length)
        {
            throw new InvalidOperationException(
                $"{errorType.Name} must be named with a non-empty Error suffix to own error codes.");
        }

        return errorName[..^"Error".Length];
    }

    private static string RemoveCaseSuffix(string caseName) =>
        caseName.EndsWith("Case", StringComparison.Ordinal)
            ? caseName[..^"Case".Length]
            : caseName;

    private static IEnumerable<string> WithoutRepeatedContext(
        IReadOnlyList<string> caseWords,
        IReadOnlyList<string> errorWords)
    {
        var repeated = 0;

        while (repeated < caseWords.Count
               && errorWords.Contains(caseWords[repeated], StringComparer.OrdinalIgnoreCase))
        {
            repeated++;
        }

        return caseWords.Skip(repeated);
    }

    private static IReadOnlyList<string> SplitWords(string name, Type sourceType)
    {
        var words = WordPattern()
            .Matches(name)
            .Select(match => match.Value)
            .ToArray();

        if (words.Length == 0
            || !string.Concat(words).Equals(name, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{sourceType.Name} is not spelled in words that convert to a stable error code.");
        }

        return words;
    }

    private static string ToSnakeCase(IEnumerable<string> words) =>
        string.Join('_', words.Select(word => word.ToLowerInvariant()));

    [GeneratedRegex(
        @"[A-Z]+(?=[A-Z][a-z]|\d|$)|[A-Z]?[a-z]+|\d+",
        RegexOptions.CultureInvariant)]
    private static partial Regex WordPattern();
}
