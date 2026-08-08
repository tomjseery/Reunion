# Reunion

Reunion is a dependency-free Result and Option library for modern .NET, designed around C# native
union cases before its public API is frozen.

The same functional type family ships in both package assets:

- On shipping .NET 10, Reunion is a conventional, validated tagged `Result`/`Option` library.
- On .NET 11, the same types additionally implement the preview C# 15 custom-union contract, so
  the compiler recognizes their named cases and can check exhaustive matches.

The public family is `Result`, `Result<TValue>`, `Result<TValue, TError>`,
`UnitResult<TError>`, and `Option<T>`. Reunion has no runtime or transitive package dependencies.

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

These are intentional tradeoffs rather than claims that another Result library cannot technically
implement custom unions. Reunion can make them foundational guarantees because its API was designed
with the union model in mind, before compatibility constraints accumulated.

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

Reunion is not the first Result library. Its focus is a ready-made, validated Result pattern whose
case model works with native C# unions without introducing a second Result implementation or
breaking the same-payload-type scenario.

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
`eng/Inspect-Package.ps1` verifies the package identity, metadata, framework assets, and empty
dependency groups before either consumer runs.

## ASP.NET Core integration

An `Option<T>` can already cross a domain boundary without knowing anything about HTTP by using
`OrFailure` from the core package. Both eager and lazy errors are supported, and `Map`, `Bind`,
`OrElse`, `ValueOr`, and `ValueOrElse` cover the other general-purpose option transformations:

```csharp
Result<User, DomainError> requiredUser = maybeUser.OrFailure(
    () => new DomainError("not_found", "The user does not exist."));
```

`OrFailure` remains a core/domain operation; the HTTP methods below deliberately map only at the
endpoint boundary.

The dependency-free functional types and the optional endpoint adapters are separate packages:

```xml
<!-- Core Result and Option types only -->
<PackageReference Include="Reunion" />

<!-- Optional ASP.NET Core endpoint integration -->
<PackageReference Include="Reunion.AspNetCore" />
```

`Reunion.AspNetCore` depends on `Reunion`; the core package never depends on ASP.NET Core. The
integration package supports two deliberately separate programming models with the same semantic
method names. Import exactly one mapping namespace in a source file:

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
static ProblemHttpResult ToProblem(DomainError error) => error switch
{
    { Code: "not_found" } => TypedResults.Problem(
        detail: error.Message,
        statusCode: StatusCodes.Status404NotFound),
    { Code: "conflict" } => TypedResults.Problem(
        detail: error.Message,
        statusCode: StatusCodes.Status409Conflict),
    _ => TypedResults.Problem(
        detail: error.Message,
        statusCode: StatusCodes.Status500InternalServerError)
};
```

The string-error `Result` families also have parameterless overloads. Because a string contains no
reliable status classification, those overloads produce an explicit `500 Internal Server Error`
problem with the string as its detail. Use the mapper overload whenever the string represents an
expected client error.

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
