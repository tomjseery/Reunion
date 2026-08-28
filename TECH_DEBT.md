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
single implicit token; getting back out to a nullable has no counterpart at all.

The evidenced need is narrow, and worth stating precisely because the obvious framing over-scopes it. A
consumer serialising an optional value object needs `Option<TStruct>` → `TStruct?`, which is exactly what
`Nullable<T>` exists for and is a genuine boundary conversion. What is *not* evidenced is
`Option<TClass>` → `TClass?`: the case that first prompted this was Concertable flattening an
`Option<TenantContact>` into two nullable strings, and that turned out to be bad modelling downstream
rather than a missing library API — fixed there by making the DTO carry the value object as one optional
group. Converting an option of a reference type to a nullable reference is usually the same smell, so
whether to ship it at all is part of the decision rather than a given.

Two constraints shape the fix if both are wanted:

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
that does not weaken it, a decision recorded on whether the reference-type case ships at all, tests across
present and absent for whichever cases do, and a published version consumers can bump to.
