# Reunion implementation plan

## Goal

Reunion will provide one dependency-free `Result`/`Option` API that works on shipping .NET 10 and
becomes a strong, compiler-recognized discriminated union when the same package is consumed from a
`.NET 11` project.

There will not be separate `StrongResult`, `NativeResult`, or compatibility types. The public types
are, and remain:

- `Result`
- `Result<TValue>`
- `Result<TValue, TError>`
- `UnitResult<TError>`
- `Option<T>`

The `net10.0` and `net11.0` assets in the NuGet package contain the same Result/Option model. The
`net11.0` asset additionally implements the C# 15 custom-union contract.

## Validated native-union design

The design was compiled and executed with .NET SDK `11.0.100-preview.6.26359.118`.

The spike verified:

- `[Union]` custom structs implementing `IUnion`.
- Named `Success<TValue>` and `Failure<TError>` cases.
- Named `Some<T>` and `None` cases.
- Exhaustive switch expressions without a fallback arm.
- Native implicit conversion from a case to its Result/Option union.
- Correct behavior for `Result<string, string>`.
- A byte tag, rather than runtime payload-type inspection, selecting the active case.
- The compiler's non-boxing pattern path: matching used `TryGetValue` and did not read the boxing
  `IUnion.Value` fallback.
- Preservation of the existing `result.TryGetValue(out var value)` API without overload ambiguity.

The last point requires a nested union member provider. The union-only `TryGetValue` overloads must
not be added directly to `Result` or `Option`, because that would make ordinary `out var` calls
ambiguous. Instead, each type implements a public nested `IUnionMembers` interface explicitly. The
C# 15 compiler discovers the case factories and non-boxing accessors through that provider, while
normal member lookup continues to see Reunion's existing raw-payload methods.

This is the intended shape for `Result<TValue, TError>` (abbreviated):

```csharp
// Shared implementation in both TFMs.
public readonly partial struct Result<TValue, TError>
    where TValue : notnull
    where TError : notnull
{
    private readonly byte tag;
    private readonly TValue? value;
    private readonly TError? error;

    public static Result<TValue, TError> Success(TValue value) { ... }
    public static Result<TValue, TError> Failure(TError error) { ... }

    // Existing ergonomic API remains available on both TFMs.
    public bool TryGetValue(out TValue value) { ... }
    public bool TryGetError(out TError error) { ... }
}

#if NET11_0_OR_GREATER
[Union]
public readonly partial struct Result<TValue, TError> :
    IUnion,
    Result<TValue, TError>.IUnionMembers
{
    public interface IUnionMembers
    {
        static Result<TValue, TError> Create(Success<TValue> value) =>
            Result<TValue, TError>.Success(value.Value);

        static Result<TValue, TError> Create(Failure<TError> error) =>
            Result<TValue, TError>.Failure(error.Error);

        object? Value { get; }
        bool HasValue { get; }
        bool TryGetValue(out Success<TValue> value);
        bool TryGetValue(out Failure<TError> value);
    }

    object? IUnion.Value => ...;
    object? IUnionMembers.Value => ...;
    bool IUnionMembers.HasValue => ...;
    bool IUnionMembers.TryGetValue(out Success<TValue> value) => ...;
    bool IUnionMembers.TryGetValue(out Failure<TError> value) => ...;
}
#endif
```

The precise syntax will continue to be covered by compiler contract tests because C# 15 remains a
preview feature.

## Case model

The union cases are distinct wrapper structs rather than raw payload types:

| Union | Cases |
| --- | --- |
| `Result` | `Success`, `Failure<string>` |
| `Result<TValue>` | `Success<TValue>`, `Failure<string>` |
| `Result<TValue, TError>` | `Success<TValue>`, `Failure<TError>` |
| `UnitResult<TError>` | `Success`, `Failure<TError>` |
| `Option<T>` | `Some<T>`, `None` |

This makes the cases discriminated and allows `Result<string, string>` to work:

```csharp
Result<string, string> result = GetResult();

string message = result switch
{
    Success<string> success => success.Value,
    Failure<string> failure => failure.Error,
};
```

Raw `TValue` and `TError` cannot be the two cases: C# native unions are unions of types, and the
compiler cannot distinguish two `string` cases by Reunion's internal tag. Distinct case wrappers
make the discrimination visible to the compiler while the Result itself retains compact tagged
storage.

The case types will be public readonly value types with runtime validation:

- `Success` is the no-payload success marker.
- `Success<TValue>` contains `Value`.
- `Failure<TError>` contains `Error`.
- `Some<T>` contains `Value`.
- `None` is the no-payload absence marker.

Every Result/Option construction boundary must revalidate wrapper contents. A caller can produce
`default(Success<string>)` even if the wrapper's normal constructor rejects null, so the receiving
factory/provider cannot blindly trust a wrapper value.

For `Option<T>`, union `HasValue` means “the union contains a case,” not “the Option is Some.” Since
`None` is a real case, union `HasValue` is true for both Some and None. Callers continue to use
`IsSome` and `IsNone` for Option semantics. `default(Option<T>)` remains `None`.

## Phase 1: migrate the functional core

Copy these files from the read-only Concertable reference into `src/Reunion`:

- `Result.cs`
- `ResultT.cs`
- `ResultTE.cs`
- `Option.cs`
- `UnitResult.cs`
- `TaskResultExtensions.cs`
- `TaskResultExtensions.Value.cs`
- `TaskResultExtensions.NoValue.cs`
- `TaskOptionExtensions.cs`
- `ResultCollectionExtensions.cs`

Migration changes:

1. Rename `Concertable.Kernel.Functional` to `Reunion`.
2. Mark the five union types `partial` so their .NET 11 contracts can live in focused files.
3. Preserve all current factories, validation, equality, hashing, formatting, combinators,
   `ConfigureAwait(false)` calls, traversal ordering, fail-fast behavior, and cancellation checks.
4. Preserve the deliberate default semantics:
   - default Result variants are uninitialized and reject operational use;
   - default Option is None.
5. Add XML documentation to the public surface.
6. Do not add application error types, HTTP mappings, ASP.NET integrations, logging, serialization
   policy, or error-taxonomy abstractions to the core package.

The source audit found no Concertable-specific dependency beyond the namespace. The migrated code
should remain package-dependency-free.

## Phase 2: tests and API baseline

Add a `tests/Reunion.Tests` project and target both `net10.0` and `net11.0` once the preview SDK is
part of the build environment.

Behavioral tests must cover:

- Success, failure, Some, and None construction.
- Null and whitespace validation.
- Default values and uninitialized Result guards.
- `Match`, `Map`, `Bind`, `MapError`, `Ensure`, taps, recovery, and Option conversion.
- Callback laziness and exception propagation.
- Equality, hashing, operators, and `ToString`.
- Task-source and async-callback overloads, including null tasks returned by callbacks.
- Collection ordering, first-failure behavior, empty inputs, selector invocation counts, and
  cancellation before and during traversal.
- Same-type `Result<T, T>` throughout the synchronous and asynchronous API.

Create and approve a public API baseline before further refactoring. This guards the large task
extension surface against accidentally losing overloads during the namespace migration.

## Phase 3: add the shared named cases

Add `Success`, `Success<TValue>`, `Failure<TError>`, `Some<T>`, and `None` to the core package for
both target frameworks.

Route all conversions through the existing validated Result/Option factories. The case wrappers
are part of the functional model and do not constitute an error taxonomy.

Add ordinary construction tests on both TFMs so the case API is consistent even where the compiler
does not yet recognize `[Union]`.

## Phase 4: add the net11.0 custom-union layer

Add conditional partial files for:

- `Result.Union.cs`
- `ResultT.Union.cs`
- `ResultTE.Union.cs`
- `UnitResult.Union.cs`
- `Option.Union.cs`

Each file is compiled only for `NET11_0_OR_GREATER` and:

1. Applies `[Union]` to the existing type.
2. Implements `IUnion`.
3. Implements a public nested `IUnionMembers` provider.
4. Defines `Create` factories for each named case and routes them to validated Reunion factories.
5. Implements `IUnion.Value` and the provider's `Value` explicitly.
6. Implements provider-only `HasValue` and `TryGetValue` members explicitly.
7. Selects every case from the existing byte tag, never from a runtime payload type test.

`IUnion.Value` necessarily boxes value-type cases when explicitly used. Ordinary native pattern
matching must use the provider's strongly typed accessors. Compiler tests will detect regressions by
instrumenting `Value` and asserting that exhaustive case matching does not read it.

## Phase 5: compiler contract tests

The `net11.0` test leg must compile consumer-style snippets, not only call the API at runtime:

- Exhaustive switches over all five union families.
- Native implicit conversion from every case type.
- `Result<string, string>` success and failure patterns.
- Reference-type and value-type payloads.
- Existing `TryGetValue(out var value)` calls remain unambiguous.
- Existing property, type, and switch patterns do not regress after `[Union]` is applied.
- Pattern matching does not use the boxing `Value` path.
- `IUnion.Value` returns only a declared case or null for an uninitialized Result.
- `default(Option<T>)` matches `None`.
- Invalid/default case wrappers cannot bypass Result/Option validation.

Run these tests against every adopted .NET 11 preview/RC SDK. Pin SDK upgrades in `global.json` and
review compiler diagnostic changes rather than floating silently between previews.

## Phase 6: project and CI configuration

After the migrated net10 implementation is green, change the library project to:

```xml
<TargetFrameworks>net10.0;net11.0</TargetFrameworks>
```

For the preview period, configure only `net11.0` with:

```xml
<PropertyGroup Condition="'$(TargetFramework)' == 'net11.0'">
  <LangVersion>preview</LangVersion>
  <EnablePreviewFeatures>true</EnablePreviewFeatures>
</PropertyGroup>
```

CI should contain:

- .NET 10 build and tests.
- .NET 11 preview build, behavioral tests, and compiler contract tests.
- `dotnet pack` plus inspection that both `lib/net10.0` and `lib/net11.0` assets are present.
- Clean consumer projects for each TFM that install the packed `.nupkg` rather than using a project
  reference.
- A public API comparison between target frameworks, allowing only the intentional union interfaces
  and supporting members on `net11.0`.

## Release plan

The existing `0.0.1` package remains a historical placeholder. It should be deprecated and
optionally unlisted, not overwritten.

Recommended sequence:

1. `0.1.0-alpha.1`: migrated core, tests, shared named cases, and initial multi-target union layer.
2. `0.1.0-alpha.N`: follow .NET 11 previews and absorb compiler/specification changes.
3. `0.1.0-beta.1`: API feature-complete and compiler-contract tests stable on a late preview.
4. `0.1.0-rc.1`: built and tested against the .NET 11 release candidate/go-live SDK.
5. `0.1.0`: first stable Reunion release after .NET 11 GA and final C# 15 validation.
6. `1.0.0`: only after the public API has received real-world use and is intentionally frozen.

Do not publish a stable package whose `net11.0` asset depends on preview union behavior. Alpha,
beta, and RC packages may expose it openly with preview-feature metadata.

## Future package family

Reunion is planned as a package family in one repository, not as a single project that accumulates
unrelated concerns.

The initial package boundary is:

- `Reunion`: the dependency-free Result, Option, UnitResult, named union cases, and their
  combinators.
- `Reunion.Errors`: reusable error building blocks generalized from Concertable, delivered from a
  separate project and NuGet package.

`Reunion.Errors` may depend on `Reunion`. A consumer that installs `Reunion.Errors` therefore gets
the core package transitively, while consumers that only want Result/Option do not inherit an error
taxonomy they did not choose.

Before implementing `Reunion.Errors`:

1. Inventory the Concertable error source separately from the functional-core migration.
2. Remove application, transport, UI, persistence, ASP.NET, and Concertable-specific assumptions.
3. Separate universally reusable error primitives from Concertable domain errors.
4. Define the minimum integration surface with `Result<TValue, TError>`; the core Result types must
   continue to accept arbitrary user-defined error types.
5. Add a dedicated `src/Reunion.Errors` project and `tests/Reunion.Errors.Tests` project to the
   existing solution.
6. Give `Reunion.Errors` its own version, package metadata, API baseline, documentation, and release
   notes. Its release cadence must not force unnecessary releases of `Reunion`.

Possible later packages should be created only around clear integration boundaries—for example an
ASP.NET integration package—not folded into `Reunion` or `Reunion.Errors`. No future package is part
of the core union implementation's critical path.

If a one-reference “full Reunion stack” becomes useful, provide a small metapackage such as
`Reunion.All` that depends on the selected Reunion packages. Do not turn the existing `Reunion`
package into both the low-level core and a changing aggregate package.

## README positioning

Position Reunion as:

> A dependency-free Result and Option library for modern .NET, designed around C# native union
> cases before its public API was frozen. Reunion works as a conventional tagged Result/Option on
> .NET 10 and exposes the same types as strong, exhaustively matched custom unions on .NET 11.

Show both styles:

```csharp
var result = Result.Success<User, Error>(user);

var message = result.Match(
    success => success.Name,
    failure => failure.Message);
```

```csharp
var message = result switch
{
    Success<User> success => success.Value.Name,
    Failure<Error> failure => failure.Error.Message,
};
```

State clearly that Reunion is not the first Result library. Its differentiator is a ready-made,
validated Result pattern whose case model is compatible with native C# unions without changing the
public Result type or breaking the same-payload-type scenario.

## Completion criteria

The first stable implementation is complete when:

- All migrated APIs have behavioral parity and documentation.
- Both target frameworks build from a clean checkout.
- The NuGet package exposes the same Result/Option family on both TFMs.
- Every `net11.0` type is recognized as a native custom union.
- Named cases are exhaustive and non-boxing in compiler-generated pattern matching.
- `Result<T, T>` is covered by compile-time and runtime tests.
- Existing raw-payload accessors remain source-compatible and unambiguous.
- The package passes installation smoke tests from both `net10.0` and `net11.0` consumers.
- The README accurately distinguishes shipping, preview, and GA behavior.
