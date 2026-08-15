# Reunion

## .NET 11 is the canonical design target

Reunion is designed for .NET 11 and native C# unions. Design every API and behavior against the
best native-union shape first. Current consumers targeting .NET 10 are migration constraints, not
design constraints, and must never make the .NET 11 API less natural, less exhaustive, or less
union-oriented.

.NET 10 remains a supported best-effort compatibility target. When the best .NET 11 implementation
cannot be expressed faithfully with shared source, use target-specific source or implementation
instead of weakening the canonical design. Keep the public surface aligned where that remains
natural; allow documented, tested target differences when a native .NET 11 capability has no honest
.NET 10 equivalent.

Before adding a cross-target workaround, check whether native unions provide a better .NET 11
solution. Prefer exhaustive union matching, native conversions, and compiler-enforced case handling
in the .NET 11 asset. Compatibility helpers that exist only to emulate those capabilities belong in
the .NET 10 asset and must not leak into or dictate the .NET 11 surface.
