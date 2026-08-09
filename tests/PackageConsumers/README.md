# Package consumers

These projects are intentionally excluded from `Reunion.slnx`. They must consume packed packages,
never library project references, so solution restore and build cannot hide packaging or dependency
errors.

The two `Reunion.Consumer` projects reference only the dependency-free core package. The two
`Reunion.Validation.Consumer` projects reference only `Reunion.Validation` and prove that it
supplies the correct transitive `Reunion` and `Reunion.Errors` dependencies. The two
`Reunion.AspNetCore.Consumer` web projects reference only `Reunion.AspNetCore`; their restore and
compilation prove that the integration package supplies the correct transitive dependencies and
ASP.NET Core framework reference. Each web consumer compiles both the `HttpResults` and MVC
namespace-selected extension surfaces. All six consumers remain excluded from `Reunion.slnx`.

The project defaults follow the planned `0.1.0-alpha.1` prerelease. CI overrides that version with
a unique prerelease identity and restores each target framework and package surface into a separate
package cache. NuGet.org is available during restore because clean or split SDK installations can
need reference packs, but `Assert-PackageConsumerRestore.ps1` verifies that every expected Reunion
package came from `artifacts/packages`. CI then builds and runs all six projects with `--no-restore`.
