using Reunion;

namespace Reunion.Tests;

public sealed class OptionNullableConversionTests
{
    [Fact]
    public void ToNullable_ValueTypePresent_ReturnsValue()
    {
        var option = Option.Some(42);

        int? actual = option.ToNullable();

        Assert.Equal(42, actual);
    }

    [Fact]
    public void ToNullable_ValueTypeAbsent_ReturnsNull()
    {
        var option = Option.None<int>();

        int? actual = option.ToNullable();

        Assert.Null(actual);
    }

    [Fact]
    public void ToNullable_ReferenceTypePresent_ReturnsValue()
    {
        var option = Option.Some("value");

        string? actual = option.ToNullable();

        Assert.Equal("value", actual);
    }

    [Fact]
    public void ToNullable_ReferenceTypeAbsent_ReturnsNull()
    {
        var option = Option.None<string>();

        string? actual = option.ToNullable();

        Assert.Null(actual);
    }

    [Fact]
    public void ToNullable_CustomValueTypePresentAndAbsent_MapsBothCases()
    {
        Assert.Equal(new Money(12.5m), Option.Some(new Money(12.5m)).ToNullable());
        Assert.Null(Option.None<Money>().ToNullable());
    }

    [Fact]
    public void ToNullable_NullableRoundTrip_PreservesBothCases()
    {
        string? presentReference = "value";
        string? absentReference = null;
        int? presentValue = 42;
        int? absentValue = null;

        Assert.Equal(presentReference, presentReference.ToOption().ToNullable());
        Assert.Equal(absentReference, absentReference.ToOption().ToNullable());
        Assert.Equal(presentValue, presentValue.ToOption().ToNullable());
        Assert.Equal(absentValue, absentValue.ToOption().ToNullable());
    }

    [Fact]
    public void ToNullable_ProjectedReferenceFromReferencePresent_ReturnsProjection()
    {
        var option = Option.Some(new Contact("user@example.com"));

        string? actual = option.ToNullable(contact => contact.Email);

        Assert.Equal("user@example.com", actual);
    }

    [Fact]
    public void ToNullable_ProjectedReferenceFromReferenceAbsent_ReturnsNull()
    {
        var option = Option.None<Contact>();

        string? actual = option.ToNullable(contact => contact.Email);

        Assert.Null(actual);
    }

    [Fact]
    public void ToNullable_ProjectedReferenceFromValuePresentAndAbsent_MapsBothCases()
    {
        Assert.Equal(new Contact("42"), Option.Some(42).ToNullable(value => new Contact(value.ToString())));
        Assert.Null(Option.None<int>().ToNullable(value => new Contact(value.ToString())));
    }

    [Fact]
    public void ToNullable_ProjectedValueFromReferencePresentAndAbsent_MapsBothCases()
    {
        Assert.Equal(5, Option.Some("value").ToNullable(value => value.Length));
        Assert.Null(Option.None<string>().ToNullable(value => value.Length));
    }

    [Fact]
    public void ToNullable_ProjectedValueFromValuePresentAndAbsent_MapsBothCases()
    {
        Assert.Equal(new Money(42m), Option.Some(42).ToNullable(value => new Money(value)));
        Assert.Null(Option.None<int>().ToNullable(value => new Money(value)));
    }

    [Fact]
    public void ToNullable_ProjectedNullMap_ThrowsArgumentNullException()
    {
        var present = Option.Some("value");
        var absent = Option.None<string>();

        Assert.Throws<ArgumentNullException>(() => present.ToNullable((Func<string, string>)null!));
        Assert.Throws<ArgumentNullException>(() => absent.ToNullable((Func<string, string>)null!));
        Assert.Throws<ArgumentNullException>(() => present.ToNullable((Func<string, int>)null!));
        Assert.Throws<ArgumentNullException>(() => absent.ToNullable((Func<string, int>)null!));
    }

    [Fact]
    public void ToNullable_SerializationBoundaryShape_ReplacesIdentityLambdaMatch()
    {
        var present = Option.Some(new Refund("refund-1"));
        var absent = Option.None<Refund>();

        Assert.Equal(
            present.Match<RefundResponse?>(refund => new RefundResponse(refund.RefundId), () => null),
            present.ToNullable(refund => new RefundResponse(refund.RefundId)));
        Assert.Equal(
            absent.Match<RefundResponse?>(refund => new RefundResponse(refund.RefundId), () => null),
            absent.ToNullable(refund => new RefundResponse(refund.RefundId)));
    }

    private sealed record Contact(string Email);

    private sealed record Refund(string RefundId);

    private sealed record RefundResponse(string RefundId);

    private readonly record struct Money(decimal Amount);
}
