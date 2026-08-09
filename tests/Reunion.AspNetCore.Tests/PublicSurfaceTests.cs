using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Reunion.AspNetCore.Tests;

public sealed class PublicSurfaceTests
{
    [Fact]
    public void HttpResultExtensions_ExposeOnlyConcreteTypedResults()
    {
        var extensionTypes = new[]
        {
            typeof(HttpResults.OptionHttpResultExtensions),
            typeof(HttpResults.ResultHttpResultExtensions)
        };

        var methods = extensionTypes.SelectMany(type => type.GetMethods()).Where(method => method.DeclaringType == method.ReflectedType);

        Assert.All(methods, method =>
        {
            Assert.NotEqual(typeof(IResult), method.ReturnType);
            Assert.True(
                method.ReturnType.IsGenericType
                && method.ReturnType.GetGenericTypeDefinition()
                    == typeof(Microsoft.AspNetCore.Http.HttpResults.Results<,>));
        });
    }

    [Fact]
    public void MvcExtensions_DoNotExposeIActionResult()
    {
        var extensionTypes = new[]
        {
            typeof(Mvc.OptionActionResultExtensions),
            typeof(Mvc.ResultActionResultExtensions)
        };

        var methods = extensionTypes.SelectMany(type => type.GetMethods()).Where(method => method.DeclaringType == method.ReflectedType);

        Assert.All(methods, method =>
        {
            Assert.NotEqual(typeof(IActionResult), method.ReturnType);
            Assert.True(
                method.ReturnType == typeof(ActionResult)
                || method.ReturnType.IsGenericType
                && method.ReturnType.GetGenericTypeDefinition() == typeof(ActionResult<>));
        });
    }

    [Fact]
    public void ProgrammingModels_UseSeparateOptInNamespacesWithMatchingMethodNames()
    {
        Assert.Equal("Reunion.AspNetCore.HttpResults", typeof(HttpResults.OptionHttpResultExtensions).Namespace);
        Assert.Equal("Reunion.AspNetCore.Mvc", typeof(Mvc.OptionActionResultExtensions).Namespace);

        var httpNames = typeof(HttpResults.OptionHttpResultExtensions)
            .GetMethods()
            .Where(method => method.DeclaringType == method.ReflectedType)
            .Select(method => method.Name)
            .Order()
            .ToArray();
        var mvcNames = typeof(Mvc.OptionActionResultExtensions)
            .GetMethods()
            .Where(method => method.DeclaringType == method.ReflectedType)
            .Select(method => method.Name)
            .Order()
            .ToArray();

        Assert.Equal(httpNames, mvcNames);
    }
}
