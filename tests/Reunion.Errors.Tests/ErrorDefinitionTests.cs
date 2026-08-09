using Reunion.Errors;

namespace Reunion.Errors.Tests;

public sealed class ErrorDefinitionTests
{
    private static readonly ErrorDefinitions<PaymentError> Definitions =
        ErrorDefinition.For<PaymentError>();

    [Fact]
    public void ExplicitFactories_CreateEveryKind()
    {
        ErrorDefinition[] definitions =
        [
            ErrorDefinition.Invalid("test.invalid", "Invalid."),
            ErrorDefinition.NotFound("test.not_found", "Not found."),
            ErrorDefinition.Conflict("test.conflict", "Conflict."),
            ErrorDefinition.Unauthenticated("test.unauthenticated", "Unauthenticated."),
            ErrorDefinition.Forbidden("test.forbidden", "Forbidden."),
            ErrorDefinition.PaymentRequired("test.payment_required", "Payment required.")
        ];

        Assert.Equal(Enum.GetValues<ErrorKind>(), definitions.Select(definition => definition.Kind));
    }

    [Fact]
    public void OwnedFactories_DeriveCodesAndMessagesWithoutCaseNesting()
    {
        var payer = Definitions.NotFound<PayerNotFound>();
        var recipient = Definitions.Conflict<RecipientUnavailable>();
        var gateway = Definitions.Invalid<HTTP2Unavailable>();

        Assert.Equal("payment.payer_not_found", payer.Code);
        Assert.Equal("Payer not found.", payer.Message);
        Assert.Equal(ErrorKind.NotFound, payer.Kind);
        Assert.Equal("payment.recipient_unavailable", recipient.Code);
        Assert.Equal("Recipient unavailable.", recipient.Message);
        Assert.Equal("payment.http_2_unavailable", gateway.Code);
        Assert.Equal("HTTP 2 unavailable.", gateway.Message);
        Assert.NotEqual(typeof(PaymentError), typeof(PayerNotFound).DeclaringType);
    }

    [Fact]
    public void OwnedFactories_RemoveRepeatedOwnerContext()
    {
        var definitions = ErrorDefinition.For<EscrowRefundError>();

        var definition = definitions.NotFound<RefundEscrowNotFound>();

        Assert.Equal("escrow.refund_not_found", definition.Code);
        Assert.Equal("Refund escrow not found.", definition.Message);
    }

    [Fact]
    public void OwnedFactories_AllowMessageAndPublishedCodeOverrides()
    {
        var definition = Definitions.PaymentRequired<PaymentRejected>(
            "The payment was rejected.");

        Assert.Equal("payment.declined", definition.Code);
        Assert.Equal("The payment was rejected.", definition.Message);
        Assert.Equal(ErrorKind.PaymentRequired, definition.Kind);
    }

    [Fact]
    public void Interface_AllowsValueTypeUnionRoots()
    {
        var definitions = ErrorDefinition.For<InventoryError>();
        InventoryError error = new(definitions.NotFound<InventoryItemNotFound>());

        Assert.Equal("inventory.item_not_found", error.Definition.Code);
        Assert.Equal(ErrorKind.NotFound, error.Definition.Kind);
    }

    [Fact]
    public void ValidationFactory_PreservesStructuredErrors()
    {
        var errors = new ValidationErrors(
            new Dictionary<string, string[]>
            {
                ["amount"] = ["Amount must be positive."]
            });

        var definition = Definitions.Validation<PaymentInvalid>(errors);

        Assert.Equal("payment.invalid", definition.Code);
        Assert.Equal("Payment invalid.", definition.Message);
        Assert.Equal(ErrorKind.Invalid, definition.Kind);
        Assert.Same(errors, definition.Errors);
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
        Assert.IsType<InvalidError>(ErrorDefinition.Invalid("test.invalid", "Invalid."));
        Assert.IsType<NotFoundError>(ErrorDefinition.NotFound("test.not_found", "Not found."));
        Assert.IsType<ConflictError>(ErrorDefinition.Conflict("test.conflict", "Conflict."));
        Assert.IsType<UnauthenticatedError>(
            ErrorDefinition.Unauthenticated("test.unauthenticated", "Unauthenticated."));
        Assert.IsType<ForbiddenError>(ErrorDefinition.Forbidden("test.forbidden", "Forbidden."));
        Assert.IsType<PaymentRequiredError>(
            ErrorDefinition.PaymentRequired("test.payment_required", "Payment required."));
    }

    private sealed record PaymentError(ErrorDefinition Definition) : IError;

    private sealed record EscrowRefundError(ErrorDefinition Definition) : IError;

    private readonly record struct InventoryError(ErrorDefinition Definition) : IError;

    private sealed record PayerNotFound;

    private sealed record RecipientUnavailable;

    private sealed record HTTP2Unavailable;

    private sealed record RefundEscrowNotFound;

    [ErrorCode("payment.declined")]
    private sealed record PaymentRejected;

    private sealed record PaymentInvalid;

    private readonly record struct InventoryItemNotFound;
}
