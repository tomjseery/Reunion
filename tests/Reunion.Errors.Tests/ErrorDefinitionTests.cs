using Reunion.Errors;

namespace Reunion.Errors.Tests;

public sealed class ErrorDefinitionTests
{
    private const string NestedCaseGuidance =
        "Derived error definition factories require {0} to be nested directly inside its IError "
        + "owner. Free-standing cases must use an explicit code/message factory such as "
        + "ErrorDefinition.NotFound(\"payment.payer_not_found\", \"Payer not found.\").";

    [Fact]
    public void ExplicitFactories_CreateEveryKind()
    {
        var errors = CreateValidationErrors();
        ErrorDefinition[] definitions =
        [
            ErrorDefinition.Invalid("test.invalid", "Invalid."),
            ErrorDefinition.NotFound("test.not_found", "Not found."),
            ErrorDefinition.Conflict("test.conflict", "Conflict."),
            ErrorDefinition.Unauthenticated("test.unauthenticated", "Unauthenticated."),
            ErrorDefinition.Forbidden("test.forbidden", "Forbidden."),
            ErrorDefinition.PaymentRequired("test.payment_required", "Payment required."),
            ErrorDefinition.Validation("test.validation", "Validation failed.", errors)
        ];

        Assert.Equal(
            [
                ErrorKind.Invalid,
                ErrorKind.NotFound,
                ErrorKind.Conflict,
                ErrorKind.Unauthenticated,
                ErrorKind.Forbidden,
                ErrorKind.PaymentRequired,
                ErrorKind.Invalid
            ],
            definitions.Select(definition => definition.Kind));
        Assert.Same(errors, Assert.IsType<ValidationError>(definitions[^1]).Errors);
    }

    [Fact]
    public void DerivedFactories_CreateEveryKindWithExactCodesAndDefaultMessages()
    {
        ErrorDefinition[] definitions =
        [
            ErrorDefinition.Invalid<PaymentError.PaymentInvalid>(),
            ErrorDefinition.NotFound<PaymentError.PayerNotFound>(),
            ErrorDefinition.Conflict<PaymentError.PaymentConflict>(),
            ErrorDefinition.Unauthenticated<PaymentError.PaymentUnauthenticated>(),
            ErrorDefinition.Forbidden<PaymentError.PaymentForbidden>(),
            ErrorDefinition.PaymentRequired<PaymentError.PaymentRequired>()
        ];

        Assert.Equal(
            [
                "payment.invalid",
                "payment.payer_not_found",
                "payment.conflict",
                "payment.unauthenticated",
                "payment.forbidden",
                "payment.required"
            ],
            definitions.Select(definition => definition.Code));
        Assert.Equal(
            [
                "Payment invalid.",
                "Payer not found.",
                "Payment conflict.",
                "Payment unauthenticated.",
                "Payment forbidden.",
                "Payment required."
            ],
            definitions.Select(definition => definition.Message));
        Assert.Equal(Enum.GetValues<ErrorKind>(), definitions.Select(definition => definition.Kind));
    }

    [Fact]
    public void DerivedFactories_UseExplicitMessagesForEveryKind()
    {
        var errors = CreateValidationErrors();
        ErrorDefinition[] definitions =
        [
            ErrorDefinition.Invalid<PaymentError.PaymentInvalid>("Invalid override."),
            ErrorDefinition.NotFound<PaymentError.PayerNotFound>("Not-found override."),
            ErrorDefinition.Conflict<PaymentError.PaymentConflict>("Conflict override."),
            ErrorDefinition.Unauthenticated<PaymentError.PaymentUnauthenticated>(
                "Unauthenticated override."),
            ErrorDefinition.Forbidden<PaymentError.PaymentForbidden>("Forbidden override."),
            ErrorDefinition.PaymentRequired<PaymentError.PaymentRequired>(
                "Payment-required override."),
            ErrorDefinition.Validation<PaymentError.PaymentValidationFailed>(
                "Validation override.",
                errors)
        ];

        Assert.Equal(
            [
                "Invalid override.",
                "Not-found override.",
                "Conflict override.",
                "Unauthenticated override.",
                "Forbidden override.",
                "Payment-required override.",
                "Validation override."
            ],
            definitions.Select(definition => definition.Message));
    }

    [Fact]
    public void DerivedFactories_RemoveRepeatedOwnerContext()
    {
        var definition = ErrorDefinition.NotFound<EscrowRefundError.RefundEscrowNotFound>();

        Assert.Equal("escrow.refund_not_found", definition.Code);
        Assert.Equal("Refund escrow not found.", definition.Message);
    }

    [Fact]
    public void DerivedFactories_PreserveAcronymAndNumberCodeGeneration()
    {
        var definition = ErrorDefinition.Invalid<PaymentError.HTTP2Unavailable>();

        Assert.Equal("payment.http_2_unavailable", definition.Code);
        Assert.Equal("HTTP 2 unavailable.", definition.Message);
    }

    [Fact]
    public void DerivedFactories_UsePublishedCodeOverrides()
    {
        var definition = ErrorDefinition.PaymentRequired<PaymentError.PaymentRejected>();

        Assert.Equal("payment.declined", definition.Code);
        Assert.Equal("Payment rejected.", definition.Message);
    }

    [Fact]
    public void DerivedFactories_AllowValueTypeOwners()
    {
        InventoryError error = new(
            ErrorDefinition.NotFound<InventoryError.InventoryItemNotFound>());

        Assert.Equal("inventory.item_not_found", error.Definition.Code);
        Assert.Equal(ErrorKind.NotFound, error.Definition.Kind);
    }

    [Fact]
    public void DerivedValidationFactories_PreserveStructuredErrors()
    {
        var errors = CreateValidationErrors();

        var definition =
            ErrorDefinition.Validation<PaymentError.PaymentValidationFailed>(errors);

        Assert.Equal("payment.validation_failed", definition.Code);
        Assert.Equal("Payment validation failed.", definition.Message);
        Assert.Equal(ErrorKind.Invalid, definition.Kind);
        Assert.Same(errors, definition.Errors);
    }

    [Fact]
    public void DerivedFactories_ReturnStrongDefinitionTypes()
    {
        var errors = CreateValidationErrors();

        Assert.IsType<InvalidError>(ErrorDefinition.Invalid<PaymentError.PaymentInvalid>());
        Assert.IsType<NotFoundError>(ErrorDefinition.NotFound<PaymentError.PayerNotFound>());
        Assert.IsType<ConflictError>(ErrorDefinition.Conflict<PaymentError.PaymentConflict>());
        Assert.IsType<UnauthenticatedError>(
            ErrorDefinition.Unauthenticated<PaymentError.PaymentUnauthenticated>());
        Assert.IsType<ForbiddenError>(ErrorDefinition.Forbidden<PaymentError.PaymentForbidden>());
        Assert.IsType<PaymentRequiredError>(
            ErrorDefinition.PaymentRequired<PaymentError.PaymentRequired>());
        Assert.IsType<ValidationError>(
            ErrorDefinition.Validation<PaymentError.PaymentValidationFailed>(errors));
    }

    [Fact]
    public void DerivedFactory_TopLevelCase_ThrowsPreciseGuidance()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            ErrorDefinition.NotFound<FreeStandingCase>);

        Assert.Equal(string.Format(NestedCaseGuidance, nameof(FreeStandingCase)), exception.Message);
    }

    [Fact]
    public void DerivedFactory_CaseNestedUnderNonError_ThrowsPreciseGuidance()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            ErrorDefinition.Conflict<NonErrorOwner.NestedCase>);

        Assert.Equal(
            string.Format(NestedCaseGuidance, nameof(NonErrorOwner.NestedCase)),
            exception.Message);
    }

    [Theory]
    [InlineData("invalid", "Error codes must contain at least two lowercase dot-separated segments.")]
    [InlineData("Test.invalid", "Error codes must contain at least two lowercase dot-separated segments.")]
    [InlineData("test.Invalid", "Error codes must contain at least two lowercase dot-separated segments.")]
    public void Definition_InvalidCode_Throws(string code, string expectedMessage)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => ErrorDefinition.Invalid(code, "Message."));

        Assert.StartsWith(expectedMessage, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ExplicitFactories_ReturnStrongDefinitionTypes()
    {
        var errors = CreateValidationErrors();

        Assert.IsType<InvalidError>(ErrorDefinition.Invalid("test.invalid", "Invalid."));
        Assert.IsType<NotFoundError>(ErrorDefinition.NotFound("test.not_found", "Not found."));
        Assert.IsType<ConflictError>(ErrorDefinition.Conflict("test.conflict", "Conflict."));
        Assert.IsType<UnauthenticatedError>(
            ErrorDefinition.Unauthenticated("test.unauthenticated", "Unauthenticated."));
        Assert.IsType<ForbiddenError>(ErrorDefinition.Forbidden("test.forbidden", "Forbidden."));
        Assert.IsType<PaymentRequiredError>(
            ErrorDefinition.PaymentRequired("test.payment_required", "Payment required."));
        Assert.IsType<ValidationError>(
            ErrorDefinition.Validation("test.validation", "Validation failed.", errors));
    }

    [Fact]
    public void DefinitionTypes_DoNotImplementApplicationErrorContract()
    {
        Type[] definitionTypes =
        [
            typeof(ErrorDefinition),
            typeof(InvalidError),
            typeof(NotFoundError),
            typeof(ConflictError),
            typeof(UnauthenticatedError),
            typeof(ForbiddenError),
            typeof(PaymentRequiredError),
            typeof(ValidationError)
        ];

        Assert.All(definitionTypes, type => Assert.False(typeof(IError).IsAssignableFrom(type)));
    }

    private static ValidationErrors CreateValidationErrors() =>
        new(
            new Dictionary<string, string[]>
            {
                ["amount"] = ["Amount must be positive."]
            });

    private sealed record PaymentError(ErrorDefinition Definition) : IError
    {
        public sealed record PaymentInvalid;

        public sealed record PayerNotFound;

        public sealed record PaymentConflict;

        public sealed record PaymentUnauthenticated;

        public sealed record PaymentForbidden;

        public sealed record PaymentRequired;

        public sealed record PaymentValidationFailed;

        public sealed record HTTP2Unavailable;

        [ErrorCode("payment.declined")]
        public sealed record PaymentRejected;
    }

    private sealed record EscrowRefundError(ErrorDefinition Definition) : IError
    {
        public sealed record RefundEscrowNotFound;
    }

    private readonly record struct InventoryError(ErrorDefinition Definition) : IError
    {
        public readonly record struct InventoryItemNotFound;
    }

    private sealed class NonErrorOwner
    {
        public sealed record NestedCase;
    }
}

internal sealed record FreeStandingCase;
