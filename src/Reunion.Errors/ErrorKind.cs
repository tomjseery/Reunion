namespace Reunion.Errors;

/// <summary>Classifies a caller-actionable application error independently of any transport.</summary>
public enum ErrorKind
{
    /// <summary>The caller supplied an invalid value or request.</summary>
    Invalid,

    /// <summary>The requested resource does not exist.</summary>
    NotFound,

    /// <summary>The operation conflicts with current application state.</summary>
    Conflict,

    /// <summary>The operation requires an authenticated caller.</summary>
    Unauthenticated,

    /// <summary>The caller is authenticated but not permitted to perform the operation.</summary>
    Forbidden,

    /// <summary>The operation cannot proceed until payment succeeds.</summary>
    PaymentRequired
}
