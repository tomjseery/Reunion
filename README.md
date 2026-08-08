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

## Conventional API on .NET 10 and .NET 11

```csharp
var result = Result.Success<User, Error>(user);

var message = result.Match(
    success => success.Name,
    failure => failure.Message);
```

Both target frameworks also expose the shared named case value types: `Success`,
`Success<TValue>`, `Failure<TError>`, `Some<T>`, and `None`. Payload-bearing cases reject `null`, and
conversions into a Result or Option revalidate the case through the normal factories.

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
    Success<User> success => success.Value.Name,
    Failure<Error> failure => failure.Error.Message,
};
```

Cases convert natively to their Result or Option union on .NET 11. Distinct wrappers keep
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

## License

MIT — see [LICENSE](./LICENSE).
