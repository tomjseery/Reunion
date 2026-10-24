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

<!-- BEGIN personal:rules sha256:f878befe2d5e1e69bc6e5cc2ed10c6e684ce894987ff4a9861c7cc2b802fe802 -->
# Personal rules

These rules govern every project that explicitly adopts `personal@tj-agents`. Adoption records the
project's personal scope; a home directory, author name or employer association never establishes it, and
employer projects stay outside this profile unless the user explicitly extends that scope. Before planning
or starting work, confirm the task is clear against each rule below.

## 1. Clean cut

Keep one current supported solution. When replacing behavior, remove the superseded names, aliases,
fallback implementations, adapters, configuration formats, compatibility branches and forwarding or
migration docs. Migrate all known callers, project declarations, installed registrations, generated
artifacts and live runtime entry points in the same cutover, and coordinate upgrades with the actual
collaborating developers. A commit that removes or renames a skill while a tracked source still names its
old `<package>:<name>` is blocked until every reference is migrated or deleted.

Verify the entry points people actually use on every supported platform, including Windows and Linux
launch paths where supported. Source edits, successful generation, passing unit tests and version commands
alone leave a migration unproven; check installed bindings and fresh runtime behavior. Record any
unverified entry point as incomplete, with its resolver and observable acceptance condition.

```text
Replace old-launch with launch:
1. Migrate the registered shell command, configuration and actual callers.
2. Delete the old entry and regenerate installed settings.
3. Invoke launch from a fresh supported Windows/Linux session and check its actual behavior.
Result: launch --version alone leaves this migration incomplete.
```

A backwards-compatibility exception exists only as an explicit user requirement. Surface it as a
deliberate policy decision, identify the affected supported behavior and reconcile this profile's scope
before implementing it, keeping the exception visible rather than inside a fallback or adapter.

## 2. Proportionate implementation

Solve the demonstrated problem with the smallest suitable implementation. Add abstractions, dependencies,
configuration switches, duplicate paths or process machinery only for a concrete current need. Prefer the
capabilities the project already selects. Verify the behavior and meaningful failure paths in proportion
to the change, with tests that prove behavior rather than restate the implementation.

## 3. Plans

Track work as a GitHub issue or project item. A plan committed under `plans/` is only the detailed
implementation plan behind one such item and links to it; working plans stay machine-local.

This policy is authored once here. A selecting project receives it at SessionStart, and adoption generates
its marked native instruction block from this body and checks it for drift. A selected plugin or an
`always` tier alone is not proof of loading.
<!-- END personal:rules -->
