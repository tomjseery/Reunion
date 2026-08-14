using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Reunion.Errors;

namespace Reunion.AspNetCore.Tests;

public sealed class ProblemDetailsExtensionsTests
{
    public static TheoryData<ErrorDefinition, int, string> Cases =>
        new()
        {
            { ErrorDefinition.Invalid("test.invalid", "Invalid."), 400, "Bad Request" },
            { ErrorDefinition.NotFound("test.not_found", "Not found."), 404, "Not Found" },
            { ErrorDefinition.Conflict("test.conflict", "Conflict."), 409, "Conflict" },
            {
                ErrorDefinition.Unauthenticated("test.unauthenticated", "Unauthenticated."),
                401,
                "Unauthorized"
            },
            { ErrorDefinition.Forbidden("test.forbidden", "Forbidden."), 403, "Forbidden" },
            {
                ErrorDefinition.PaymentRequired("test.payment_required", "Payment required."),
                402,
                "Payment Required"
            }
        };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Create_MapsEveryDefinitionType(
        ErrorDefinition definition,
        int expectedStatus,
        string expectedTitle)
    {
        var details = ProblemDetails.Create(new TestError(definition));

        Assert.IsType<ProblemDetails>(details);
        Assert.Equal(expectedStatus, details.Status);
        Assert.Equal(expectedTitle, details.Title);
        Assert.Equal(definition.Message, details.Detail);
        Assert.Equal(definition.Code, details.Extensions[ProblemDetailsExtensions.CodeExtensionKey]);
    }

    [Fact]
    public void Create_ValidationError_PreservesStructuredErrors()
    {
        var validation = ErrorDefinition.Validation(
            "test.invalid",
            "The request is invalid.",
            new ValidationErrors(
                new Dictionary<string, string[]>
                {
                    ["name"] = ["Name is required."]
                }));

        var details = ProblemDetails.Create(new TestError(validation));

        var validationDetails = Assert.IsType<ValidationProblemDetails>(details);
        Assert.Equal(StatusCodes.Status400BadRequest, validationDetails.Status);
        Assert.Equal("Name is required.", Assert.Single(validationDetails.Errors["name"]));
        Assert.Equal(
            "test.invalid",
            validationDetails.Extensions[ProblemDetailsExtensions.CodeExtensionKey]);
    }

    [Fact]
    public void Create_NullErrorOrDefinition_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => ProblemDetails.Create<TestError>(null!));
        Assert.Throws<InvalidOperationException>(
            () => ProblemDetails.Create(new NullDefinitionError()));
    }

    private sealed record TestError(ErrorDefinition Definition) : IError;

    private sealed class NullDefinitionError : IError
    {
        public ErrorDefinition Definition => null!;
    }
}
