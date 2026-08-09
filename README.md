# Reunion

Reunion is a dependency-free, union-first Result and Option library for modern .NET.

Result libraries have traditionally been designed as runtime encodings of discriminated unions
because C# did not have native union support. Reunion starts from the union design itself: its
cases, invariants, and public shape are designed as a native discriminated union first, then exposed
through the same functional API on targets where native unions are unavailable.

The same functional type family ships in both package assets:

- On shipping .NET 10, Reunion is a conventional, validated tagged `Result`/`Option` library.
- On .NET 11, the same types additionally implement the preview C# 15 custom-union contract, so
  the compiler recognizes their named cases and can check exhaustive matches.

The core family is `Result`, `Result<TValue>`, `Result<TValue, TError>`,
`UnitResult<TError>`, and `Option<T>`. The `Reunion` core package has no runtime or transitive package
dependencies; optional error, validation, and ASP.NET Core concerns live in companion packages.

> [!IMPORTANT]
> The .NET 11 custom-union support currently depends on preview language and runtime features. It is
> intended for preview packages until .NET 11 and C# 15 reach general availability. The .NET 10 API
> uses shipping language/runtime behavior and does not require preview features.
>
> The pinned Preview 6 compiler unwraps existing property patterns such as
> `result is { IsSuccess: true }` to the contained case and rejects a typed
> `Result<TValue, TError> { ... }` pattern. Use `IsSuccess`/`IsFailure` directly, `Match`, or named
> case patterns in the preview asset. The current C# proposal specifies instance-first behavior for
> these patterns, but Reunion will not claim that compatibility until it passes against a released
> SDK. See [dotnet/roslyn#83055](https://github.com/dotnet/roslyn/issues/83055).

## What Reunion optimizes for

Reunion is deliberately strict at the boundaries of the type system:

- Results are created explicitly; raw values and errors do not implicitly become Results.
- Payloads are read through `Match`, `TryGetValue`, and `TryGetError`; there is no accessor that
  throws merely because the caller selected the wrong case.
- Success and failure use distinct case types, so `Result<T, T>` remains fully discriminated.
- A default Result is an explicit uninitialized union state and rejects operational use; a default
  Option is `None`.
- The functional and native-union views use the same storage, validation, cases, and semantics.

These are foundational guarantees because Reunion's API was designed from the union model outward,
before compatibility constraints accumulated. Native matching is therefore another view of the
same Result and Option types, not a second implementation layered over a conventional Result API.

## Conventional API on .NET 10 and .NET 11

```csharp
var result = Result<User, Error>.Success(user);

var message = result.Match(
    success => success.Name,
    failure => failure.Message);
```

Both target frameworks also expose the shared named case value types: `Success`,
`Success<TValue>`, `Failure<TError>`, `Some<T>`, and `None`. Payload-bearing cases reject `null`,
and `Failure<string>` also rejects empty or whitespace errors. Named cases implicitly convert to
their compatible Result or Option on both targets: .NET 10 uses case-only compatibility operators,
while .NET 11 uses native union conversions. Every conversion revalidates through the normal
Result/Option factories. Cases have value equality, readable formatting, and payload
deconstruction:

```csharp
Result<User, Error> found = new Success<User>(user);
Result<User, Error> failed = new Failure<Error>(error);

var description = result.Match(
    static value => $"found {value.Name}",
    static error => $"failed: {error.Message}");

var query =
    from user in FindUser(id)
    from account in FindAccount(user.AccountId)
    select (user, account);
```

`Result<TValue>`, `Result<TValue, TError>`, and `Option<T>` support this minimal LINQ query syntax
by forwarding to their existing fail-fast `Map` and `Bind` semantics.

## Native union matching on .NET 11

A .NET 11 consumer enables preview features and can exhaustively match the same Result type:

```xml
<PropertyGroup>
  <TargetFramework>net11.0</TargetFramework>
  <LangVersion>preview</LangVersion>
  <EnablePreviewFeatures>true</EnablePreviewFeatures>
</PropertyGroup>
```

```csharp
Result<User, Error> result = GetUser();

var message = result switch
{
    Success<User>(var user) => user.Name,
    Failure<Error>(var error) => error.Message,
};
```

Cases convert natively to their Result or Option union on .NET 11; the equivalent source syntax is
provided by compatibility operators on .NET 10. Distinct wrappers keep
`Result<string, string>` correctly discriminated, while Reunion's existing
`TryGetValue(out var value)` API remains unambiguous. Compiler-generated matching uses strongly
typed case accessors rather than the boxing `IUnion.Value` fallback.

Its focus is a ready-made, validated Result pattern whose case model works with native C# unions
without introducing a second Result implementation or breaking the same-payload-type scenario.

## Development status

Reunion is under pre-release development. The repository pins the exact .NET 11 Preview 6 SDK used
to validate the compiler contract. Release builds test the net10 asset on the .NET 10 runtime and
the net11 asset on the pinned preview runtime, then install a locally packed NuGet package into
clean consumer projects for both target frameworks.

The default package version is the planned first prerelease, `0.1.0-alpha.1`. The published
historical `0.0.1` placeholder remains unchanged and must not be overwritten. CI uses a unique
prerelease version for every run so package-consumer checks cannot resolve a stale local build.

`Reunion.slnx` contains the library, behavioral/compiler tests, and the target-framework API
comparison tool. The projects under `tests/PackageConsumers` are intentionally excluded: CI
restores them only after packing, with a run-unique Reunion version and isolated package caches.
NuGet.org remains available for SDK reference packs needed by clean or split SDK installations;
the restore check proves Reunion itself came from the generated package source.
The package inspection scripts verify each package's identity, metadata, framework assets, and
dependency groups before the clean consumers run.

Future .NET target frameworks consume the nearest compatible package asset selected by NuGet. A
future framework may therefore use the `net11.0` asset, but Reunion will validate each new SDK and
compiler contract explicitly before claiming native-union compatibility for that framework.

## Typed application errors

`Reunion.Errors` is an optional, transport-neutral companion package. It does not define an
application's error union and does not depend on a union generator. Instead, a manual closed
hierarchy, a generated discriminated union, or a native C# union can implement the same small
`IError` contract and expose a safe `ErrorDefinition`:

```xml
<PackageReference Include="Reunion.Errors" />
```

On .NET 10, the portable manual-hierarchy form is:

```csharp
using Reunion.Errors;

public abstract record UserLookupError : IError
{
    private UserLookupError()
    {
    }

    public ErrorDefinition Definition => this switch
    {
        UserNotFound => ErrorDefinition.NotFound<UserNotFound>(),
        EmailInvalid => ErrorDefinition.Invalid<EmailInvalid>("The email address is invalid."),
        _ => throw new InvalidOperationException("Unknown error case.")
    };

    public sealed record UserNotFound : UserLookupError;
    public sealed record EmailInvalid : UserLookupError;
}
```

The defensive arm belongs to this .NET 10 manual inheritance example because the compiler does not
prove that hierarchy exhaustive. A generated union, or a native C# union on .NET 11, can keep the
same direct factory calls while omitting that arm when its compiler-checked match is exhaustive.

The direct generic factories derive the owning error type from each case's immediate declaring
type, which must implement `IError`. Nesting cases directly inside their owner makes that
relationship unambiguous. The examples above produce `user.lookup_not_found` / `User not found.`
and `user.lookup_email_invalid` / the explicit message. Use
`[ErrorCode("user.lookup_missing")]` on a case only when a published code intentionally differs
from the convention.

Free-standing cases cannot encode their owning error type. Use the explicit code/message factories
for them instead:

```csharp
var definition = ErrorDefinition.NotFound(
    "payment.payer_not_found",
    "Payer not found.");
```

Definitions are strong records—`NotFoundError`, `ConflictError`, `UnauthenticatedError`,
`ForbiddenError`, `PaymentRequiredError`, `InvalidError`, and `ValidationError`—so callers can
pattern-match the specific definition when useful. `ErrorKind` supplies the corresponding compact
semantic classification. `Unauthenticated` deliberately represents a missing/invalid identity
(HTTP 401 at that boundary); `Forbidden` represents an authenticated caller without permission.
Structured validation uses immutable, non-empty `ValidationErrors` rather than flattening field
errors into a message:

```csharp
var errors = new ValidationErrors(new Dictionary<string, string[]>
{
    ["email"] = ["The email address is invalid."]
});

var definition = ErrorDefinition.Validation<UserLookupError.EmailInvalid>(errors);
```

`IError` belongs on the application's union/root value, not on each definition as a replacement for
that union. This keeps `Result<TValue, TError>` strongly typed while avoiding repeated codes,
classifications, and consistent messages at every return site.

## Structured validation results

`Reunion.Validation` is an optional union-first package for validators that return structured
field errors without a success payload:

```xml
<PackageReference Include="Reunion.Validation" />
```

It depends on `Reunion` and `Reunion.Errors`; neither dependency points back to validation. Its
closed case model is deliberately fixed:

```text
ValidationResult = Valid | Invalid(ValidationErrors)
```

`ValidationResult` is distinct from the general-purpose `UnitResult<TError>`. It gives validation a
specific vocabulary, permanently fixes the invalid payload to non-empty `ValidationErrors`, and
adds accumulation semantics for independent validators. It does not replace application-owned
domain errors; applications can map validation errors into their own typed error union at a
boundary.

`Combine` accumulates every invalid input. Distinct fields are preserved, and messages for the same
field stay in left-to-right order with duplicates retained. The inputs remain unchanged; combining
two invalid values creates a new immutable `ValidationErrors` collection.

Validation converts explicitly to the Result family. There is no implicit conversion from raw
`ValidationErrors`; only `Valid` and `Invalid` convert to `ValidationResult`:

```csharp
ValidationResult validation = validator.Validate(request);

if (validation.IsInvalid)
    return validation.ToResult();
```

Value-bearing methods can use the named failure returned by `TryGetFailure` for an early return:

```csharp
if (validation.TryGetFailure(out var failure))
    return failure;

if (validation.TryGetFailure(
    errors => new CommandError.ValidationFailed(errors),
    out var domainFailure))
{
    return domainFailure;
}
```

`Match` is the portable, branch-complete API on both target frameworks:

```csharp
string message = validation.Match(
    valid: () => "valid",
    invalid: FormatErrors);
```

The net11 asset also supports compiler-proven exhaustive matching:

```csharp
string message = validation switch
{
    Valid => "valid",
    Invalid(var errors) => FormatErrors(errors)
};
```

`ValidationResult` contains exactly one `UnitResult<ValidationErrors>` field. It adds no allocation
or storage overhead relative to `UnitResult<ValidationErrors>`. Delegate creation and immutable
error accumulation may still allocate depending on the operation and call site; combining two
invalid values necessarily creates the new immutable error collection described above.

## ASP.NET Core integration

An `Option<T>` can already cross a domain boundary without knowing anything about HTTP by using
`OrFailure` from the core package. Both eager and lazy errors are supported, and `Map`, `Bind`,
`OrElse`, `ValueOr`, and `ValueOrElse` cover the other general-purpose option transformations:

```csharp
Option<User> userOption = FindUser(userId);
Result<User, DomainError> requiredUser = userOption.OrFailure(
    () => new DomainError("not_found", "The user does not exist."));
```

`OrFailure` remains a core/domain operation; the HTTP methods below deliberately map only at the
endpoint boundary.

The dependency-free functional types and the optional endpoint adapters are separate packages:

```xml
<!-- Core Result and Option types only -->
<PackageReference Include="Reunion" />

<!-- Optional transport-neutral typed error definitions -->
<PackageReference Include="Reunion.Errors" />

<!-- Optional structured validation results and accumulation -->
<PackageReference Include="Reunion.Validation" />

<!-- Optional ASP.NET Core endpoint integration -->
<PackageReference Include="Reunion.AspNetCore" />
```

`Reunion.Errors` is independent of the core functional types. `Reunion.Validation` depends outward
on `Reunion` and `Reunion.Errors`, while `Reunion.AspNetCore` continues to depend only on those same
two lower-level packages and does not depend on validation. No dependency points back to an
integration package.
The ASP.NET Core package supports two deliberately separate programming models with the same
semantic method names. Import exactly one mapping namespace in a source file:

```csharp
// Concrete TypedResults and Results<T1, T2> unions.
using Reunion.AspNetCore.HttpResults;

// MVC ActionResult<T> and ActionResult.
using Reunion.AspNetCore.Mvc;
```

The `HttpResults` surface works in Minimal APIs and in API controllers. The MVC surface retains
MVC action-result execution, configured output formatters, and content negotiation. Importing both
mapping namespaces makes identical extension calls ambiguous by design rather than silently
selecting an HTTP programming model.

### Minimal API examples

GET with `200 OK` or `404 Not Found`:

```csharp
app.MapGet("/users/{id:int}", async (int id, UserService service) =>
    (await service.FindUser(id)).ToOkOrNotFound());
```

GET with `200 OK` or `204 No Content`:

```csharp
app.MapGet("/users/{id:int}/avatar", async (int id, UserService service) =>
    (await service.FindAvatar(id)).ToOkOrNoContent());
```

GET with `200 OK` or a caller-mapped problem:

```csharp
app.MapGet("/users/{id:int}", async (int id, UserService service) =>
    (await service.GetUser(id)).ToOkOrProblem(ToProblem));
```

POST with `201 Created`, a response body and a `Location` header, or a caller-mapped problem:

```csharp
app.MapPost("/users", async (CreateUserRequest request, UserService service) =>
    (await service.CreateUser(request)).ToCreatedOrProblem(
        user => $"/users/{user.Id}",
        ToProblem));
```

DELETE with `204 No Content` or a caller-mapped problem:

```csharp
app.MapDelete("/users/{id:int}", async (int id, UserService service) =>
    (await service.DeleteUser(id)).ToNoContentOrProblem(ToProblem));
```

The application owns the mapping from its error type to HTTP semantics:

```csharp
static ProblemDetails ToProblem(DomainError error) => error switch
{
    { Code: "not_found" } => new ProblemDetails
    {
        Detail = error.Message,
        Status = StatusCodes.Status404NotFound
    },
    { Code: "conflict" } => new ProblemDetails
    {
        Detail = error.Message,
        Status = StatusCodes.Status409Conflict
    },
    _ => new ProblemDetails
    {
        Detail = error.Message,
        Status = StatusCodes.Status500InternalServerError
    }
};
```

String-error Results always require an explicit problem mapper. Reunion cannot know whether an
arbitrary error string is safe to disclose or which HTTP status it represents, so it never writes
that string to a response automatically. Caller-supplied mappers return `ProblemDetails` and must
set its `Status` in both programming models.

For `TError : IError`, the mapper can be omitted. The integration derives a `ProblemDetails` from
the error definition, includes its stable code in the `code` extension, and applies this boundary
policy:

| Error kind | HTTP status |
|---|---:|
| `Invalid` | 400 |
| `NotFound` | 404 |
| `Conflict` | 409 |
| `Unauthenticated` | 401 |
| `Forbidden` | 403 |
| `PaymentRequired` | 402 |

```csharp
app.MapGet("/users/{id:int}", async (int id, UserService service) =>
    (await service.GetUser(id)).ToOkOrProblem());
```

A `ValidationError` becomes `ValidationProblemDetails` and preserves its field-indexed errors.
Minimal API results and MVC results also participate in ASP.NET Core's optional
`IProblemDetailsService`, so an application's configured problem-details customization is applied;
both programming models retain a problem-details fallback when that service or a writer is
unavailable. MVC problem responses also include the request path as `instance` and the current
activity or HTTP trace identifier as `traceId`.

For other success statuses, `ToResults` keeps the concrete Minimal/API-controller result union while
using the same automatic typed-error dispatch:

```csharp
app.MapPost("/jobs", async (JobRequest request, JobService service) =>
    (await service.Start(request)).ToResults(
        job => TypedResults.Accepted($"/jobs/{job.Id}", job)));
```

The MVC equivalent is `ToActionResult(successMapper)`. `ToOkOrProblem`,
`ToCreatedOrProblem`, and the unit-result conveniences delegate to these generic terminal adapters.

### MVC controller example

MVC uses the same call names after importing `Reunion.AspNetCore.Mvc`:

```csharp
[HttpGet("{id:int}")]
public async Task<ActionResult<User>> Get(int id) =>
    (await service.GetUser(id)).ToOkOrProblem(error => new ProblemDetails
    {
        Status = error.Code == "not_found"
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status500InternalServerError,
        Detail = error.Message
    });
```

MVC error mappers return `ProblemDetails` with an explicit status. Subtypes such as
`ValidationProblemDetails`, including their structured errors and extensions, are preserved.

## License

MIT — see [LICENSE](./LICENSE).
