using Reunion.ApiComparison;

if (args.Length is not 2)
{
    Console.Error.WriteLine("Usage: Reunion.ApiComparison <net10 Reunion.dll> <net11 Reunion.dll>");
    return 2;
}

using var net10 = LoadedAssembly.Open("Reunion-net10", Path.GetFullPath(args[0]));
using var net11 = LoadedAssembly.Open("Reunion-net11", Path.GetFullPath(args[1]));

var errors = ApiComparer.Compare(net10.Assembly, net11.Assembly);
if (errors.Count is not 0)
{
    foreach (var error in errors)
    {
        Console.Error.WriteLine(error);
    }

    return 1;
}

Console.WriteLine("Public APIs match; only IUnion and the five validated IUnionMembers providers differ.");
return 0;
