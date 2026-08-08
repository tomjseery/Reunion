# Reunion

A `Result`/`Option` library for .NET.

Reunion is being built to work today on shipping .NET, and to adopt C#'s native union types
(`[Union]`/`IUnion`, landing in C# 15 / .NET 11) as they stabilize — without the ambiguity bugs that
hit a naive `union Result<TValue, TError>(TValue, TError)` declaration when `TValue` and `TError`
happen to be the same type. Dispatch is tag-based, not type-based, so it stays correct regardless of
what the two type parameters are instantiated with.

Status: early scaffolding, no code yet. Core types and combinators are planned next.

## License

MIT — see [LICENSE](./LICENSE).
