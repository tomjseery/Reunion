# Reunion — Technical Debt

Known gaps in the shipped API and the shape a fix has to take. An entry is deleted by the change that
resolves it.

---

## MED

### `Option<T>` has no exit to `T?`, so the conversion is one-way

`Option.Conversions.cs` declares three implicit conversions and all three point *inward*: `[AllowNull] T →
Option<T>` (mapping null to `None`), `Some<T> → Option<T>`, and `None → Option<T>`. Nothing converts
outward. The only ways to leave an option are `Match`, `TryGetValue`, `ValueOr` and `ValueOrElse`, none of
which yields `T?`.

The asymmetry is the problem, not the absence on its own. Getting *into* an option from a nullable is a
single implicit token; getting back out to a nullable costs an identity-lambda
`Match<T?>(value => value, () => null)` or a multi-line `TryGetValue` block. Consumers hit this wherever an
option-returning service meets a DTO with nullable members, which is most write boundaries — Concertable
carries the shape in `SetupCheckoutStep` and `VerificationService`, and it spreads by copy because there is
nothing better to reach for.

Two constraints shape the fix:

- **`T` is `notnull`, so `T?` is two different types.** `Nullable<T>` for `where T : struct`, a nullable
  reference for `where T : class`. One member cannot serve both.
- **A conversion operator cannot be constraint-overloaded.** `explicit operator T?(Option<T>)` cannot be
  declared twice differing only by constraint, so the operator route is closed. Extension methods *can* be:
  two `ToNullable<T>(this Option<T>)` declarations differing by `where T : struct` / `where T : class` are
  distinct signatures and legal.

Per this repo's .NET 11 design rule the native-union shape is the one to settle first. `Option.Union.cs`
already models `Some<T>` and `None` as union cases, so an exhaustive `switch` over them is the route to
evaluate before any named helper — the question is whether that is the canonical answer with a named exit
existing only as `net10.0` compatibility, or whether the named exit is canonical on both targets.

**Resolves when:** the outward conversion has a settled shape on `net11.0` first, a `net10.0` equivalent
that does not weaken it, tests covering struct and reference `T` across present and absent, and a published
version consumers can bump to.
