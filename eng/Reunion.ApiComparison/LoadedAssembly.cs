using System.Reflection;
using System.Runtime.Loader;

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
        return new LoadedAssembly(context, context.LoadFromAssemblyPath(path));
    }

    public void Dispose() => this.context.Unload();
}
