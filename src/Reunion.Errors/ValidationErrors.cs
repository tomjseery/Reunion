using System.Collections.ObjectModel;

namespace Reunion.Errors;

/// <summary>Represents a validated, immutable collection of field-specific error messages.</summary>
public sealed class ValidationErrors : IEquatable<ValidationErrors>
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> errors;

    /// <summary>Initializes validation errors from field-and-message pairs.</summary>
    public ValidationErrors(IEnumerable<KeyValuePair<string, string>> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var messages = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var error in errors)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(error.Key);
            ArgumentException.ThrowIfNullOrWhiteSpace(error.Value);

            if (!messages.TryGetValue(error.Key, out var values))
            {
                values = [];
                messages.Add(error.Key, values);
            }

            values.Add(error.Value);
        }

        if (messages.Count == 0)
        {
            throw new ArgumentException(
                "Validation errors must contain at least one message.",
                nameof(errors));
        }

        this.errors = new ReadOnlyDictionary<string, IReadOnlyList<string>>(
            messages.ToDictionary(
                error => error.Key,
                error => (IReadOnlyList<string>)error.Value.AsReadOnly(),
                StringComparer.Ordinal));
    }

    /// <summary>Initializes validation errors from a field-indexed dictionary.</summary>
    public ValidationErrors(IReadOnlyDictionary<string, string[]> errors)
        : this(Flatten(errors))
    {
    }

    /// <summary>Gets the field-indexed validation messages.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Errors => this.errors;

    /// <summary>Creates an isolated dictionary suitable for serialization.</summary>
    public IReadOnlyDictionary<string, string[]> ToDictionary() =>
        new ReadOnlyDictionary<string, string[]>(
            this.errors.ToDictionary(
                error => error.Key,
                error => error.Value.ToArray(),
                StringComparer.Ordinal));

    /// <inheritdoc />
    public bool Equals(ValidationErrors? other)
    {
        if (other is null || this.errors.Count != other.errors.Count)
            return false;

        return this.errors.All(error =>
            other.errors.TryGetValue(error.Key, out var messages)
            && error.Value.SequenceEqual(messages, StringComparer.Ordinal));
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is ValidationErrors other && this.Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (var error in this.errors.OrderBy(error => error.Key, StringComparer.Ordinal))
        {
            hash.Add(error.Key, StringComparer.Ordinal);

            foreach (var message in error.Value)
                hash.Add(message, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }

    private static IEnumerable<KeyValuePair<string, string>> Flatten(
        IReadOnlyDictionary<string, string[]> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        foreach (var error in errors)
        {
            ArgumentNullException.ThrowIfNull(error.Value);

            if (error.Value.Length == 0)
            {
                throw new ArgumentException(
                    "Each validation field must contain at least one message.",
                    nameof(errors));
            }

            foreach (var message in error.Value)
                yield return new(error.Key, message);
        }
    }
}
