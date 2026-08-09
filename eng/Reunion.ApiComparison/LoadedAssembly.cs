using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;

namespace Reunion.ApiComparison;

internal sealed class LoadedAssembly : IDisposable
{
    private readonly AssemblyLoadContext context;

    private LoadedAssembly(AssemblyLoadContext context, Assembly assembly)
    {
        this.context = context;
        this.Assembly = assembly;
    }

    public Assembly Assembly { get; }

    public static LoadedAssembly Open(string name, string path)
    {
        var context = new AssemblyLoadContext(name, isCollectible: true);
        var directory = Path.GetDirectoryName(path)!;
        var aspNetCoreDirectory = FindAspNetCoreDirectory(path);
        context.Resolving += (_, assemblyName) =>
        {
            var dependencyPath = Path.Combine(directory, assemblyName.Name + ".dll");
            if (File.Exists(dependencyPath))
                return context.LoadFromAssemblyPath(dependencyPath);

            if (aspNetCoreDirectory is not null)
            {
                dependencyPath = Path.Combine(
                    aspNetCoreDirectory,
                    assemblyName.Name + ".dll");
                if (File.Exists(dependencyPath))
                    return context.LoadFromAssemblyPath(dependencyPath);
            }

            return null;
        };
        return new LoadedAssembly(context, context.LoadFromAssemblyPath(path));
    }

    private static string? FindAspNetCoreDirectory(string assemblyPath)
    {
        var targetMajor = GetTargetMajor(assemblyPath);
        if (targetMajor is null)
            return null;

        return GetDotnetRoots()
            .Select(root => Path.Combine(root, "shared", "Microsoft.AspNetCore.App"))
            .Where(Directory.Exists)
            .SelectMany(Directory.EnumerateDirectories)
            .Select(directory => new
            {
                Directory = directory,
                Version = ParseVersion(Path.GetFileName(directory))
            })
            .Where(candidate => candidate.Version?.Major == targetMajor)
            .OrderByDescending(candidate => candidate.Version)
            .Select(candidate => candidate.Directory)
            .FirstOrDefault();
    }

    private static int? GetTargetMajor(string assemblyPath)
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(assemblyPath)!);
        while (directory is not null)
        {
            var match = Regex.Match(directory.Name, "^net(?<major>[0-9]+)(?:\\.|$)");
            if (match.Success)
                return int.Parse(match.Groups["major"].Value);

            directory = directory.Parent;
        }

        return null;
    }

    private static IEnumerable<string> GetDotnetRoots()
    {
        var roots = new[]
        {
            Environment.GetEnvironmentVariable("DOTNET_ROOT"),
            Environment.GetEnvironmentVariable("DOTNET_ROOT_X64"),
            GetCurrentDotnetRoot(),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "dotnet"),
            "/usr/share/dotnet",
            "/usr/local/share/dotnet"
        };

        return roots
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .Select(root => Path.GetFullPath(root!))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string GetCurrentDotnetRoot()
    {
        var runtimeDirectory = new DirectoryInfo(
            System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory());
        return runtimeDirectory.Parent!.Parent!.Parent!.FullName;
    }

    private static Version? ParseVersion(string value)
    {
        var prereleaseSeparator = value.IndexOf('-');
        if (prereleaseSeparator >= 0)
            value = value[..prereleaseSeparator];

        return Version.TryParse(value, out var version) ? version : null;
    }

    public void Dispose() => this.context.Unload();
}
