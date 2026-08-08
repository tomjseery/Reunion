# Package consumers

These projects are intentionally excluded from `Reunion.slnx`. They must consume a packed Reunion
package, never the library project reference, so solution restore and build cannot hide packaging
or dependency errors.

The project defaults follow the planned `0.1.0-alpha.1` prerelease. CI overrides that version with
a unique prerelease identity and restores each target framework into a separate package cache.
NuGet.org is available during restore because clean or split SDK installations can need reference
packs, but `Assert-PackageConsumerRestore.ps1` verifies that Reunion came from `artifacts/packages`.
CI then builds and runs both projects with `--no-restore`.
