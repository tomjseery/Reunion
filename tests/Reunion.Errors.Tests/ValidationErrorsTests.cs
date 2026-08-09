using Reunion.Errors;

namespace Reunion.Errors.Tests;

public sealed class ValidationErrorsTests
{
    [Fact]
    public void Constructor_GroupsMessagesAndReturnsIsolatedArrays()
    {
        var errors = new ValidationErrors(
        [
            new("name", "Name is required."),
            new("name", "Name is too long."),
            new("email", "Email is invalid.")
        ]);

        var first = errors.ToDictionary();
        var second = errors.ToDictionary();
        first["name"][0] = "changed";

        Assert.Equal(["Name is required.", "Name is too long."], second["name"]);
        Assert.Equal(["Email is invalid."], second["email"]);
    }

    [Fact]
    public void Equality_IsStructuralAndOrderSensitiveWithinAField()
    {
        var left = new ValidationErrors(
            new Dictionary<string, string[]>
            {
                ["name"] = ["Required.", "Too long."],
                ["email"] = ["Invalid."]
            });
        var same = new ValidationErrors(
            new Dictionary<string, string[]>
            {
                ["email"] = ["Invalid."],
                ["name"] = ["Required.", "Too long."]
            });
        var reordered = new ValidationErrors(
            new Dictionary<string, string[]>
            {
                ["name"] = ["Too long.", "Required."],
                ["email"] = ["Invalid."]
            });

        Assert.Equal(left, same);
        Assert.Equal(left.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(left, reordered);
    }

    [Fact]
    public void Constructor_RejectsEmptyOrInvalidMessages()
    {
        Assert.Throws<ArgumentException>(() => new ValidationErrors([]));
        Assert.Throws<ArgumentException>(
            () => new ValidationErrors([new KeyValuePair<string, string>("", "Message.")]));
        Assert.Throws<ArgumentException>(
            () => new ValidationErrors([new KeyValuePair<string, string>("field", " ")]));
        Assert.Throws<ArgumentException>(() =>
            new ValidationErrors(
                new Dictionary<string, string[]>
                {
                    ["empty"] = [],
                    ["valid"] = ["Message."]
                }));
    }
}
