# Reunion technical debt

## `ValidationResult` lacks the fluent composition surface of its wrapped unit result

`ValidationResult` is represented as a one-field wrapper over `UnitResult<ValidationErrors>`, but it
only exposes observation, accumulation, and explicit `ToResult` conversions. A caller that has
finished accumulating independent validation failures cannot directly continue with the ordinary
unit-result operations: `Bind`, `BindAsync`, `MapError`, `Tap`, `TapError`, `Recover`,
`RecoverWith`, and their task/async variants. It must first expose the implementation relationship
with `ToResult()`, then compose the resulting `UnitResult<ValidationErrors>` or
`Result<TValue, TError>`.

That conversion is useful as an explicit escape hatch, but requiring it for every composed workflow
makes the specialized wrapper feel less capable than the carrier it deliberately specializes.
`Combine` and collection `Combine` should remain the explicit accumulating operations. Once a caller
chooses ordinary fluent composition, invalid should short-circuit and valid should invoke the next
operation, matching `UnitResult` semantics. Guard-style `TryGetErrors`/`TryGetFailure`, terminal
`Match`, explicit `ToResult`, and fluent composition are all valid APIs for different call-site
shapes; adding parity should not deprecate one style in favour of another.

Concertable exposes the concrete consumer cost. Its Ticket purchase and checkout pipelines both need
the same operation: map invalid `ValidationErrors` into an operation-owned error, otherwise carry the
validated `ConcertDto` into the next stage. Without direct composition this becomes either repeated
`TryGetFailure` guards plus `return concert`, repeated `ToResult(() => concert, errorMapper)` bridges,
or a consumer-local generic wrapper. All three spellings repeat carrier mechanics that the validation
abstraction should own; the last one also fragments Reunion's vocabulary across consumers.

The design should also evaluate a lossless implicit conversion from `ValidationResult` to
`UnitResult<ValidationErrors>`. `Valid` can map to success and `Invalid(errors)` to failure without an
error mapper or lost information, which could simplify assignments and arguments expecting the core
carrier. This is complementary, not sufficient: C# does not use a user-defined conversion to discover
`UnitResult` instance or extension methods for `validation.Bind(...)`, and conversions to an
application-owned error or value-bearing Result still require explicit mapping inputs.

**Resolves when:** design and implement the semantically applicable `UnitResult<ValidationErrors>`
composition surface directly on `ValidationResult`, including `Task<ValidationResult>` and async
callback parity. Operations that keep validation semantics should preserve `ValidationResult` and
its fixed `Valid | Invalid(ValidationErrors)` vocabulary; operations that produce a value or map to
an application-owned error should return the corresponding Result-family carrier without requiring
an explicit preliminary `ToResult`. Document the accumulation-versus-short-circuit boundary, update
the public API baselines and package README, and add parity tests covering valid, invalid, default,
null-callback, faulted-task, and cancelled-task behavior. Include a value-carrying, application-error
mapping pipeline equivalent to Concertable's Ticket validation as a package-consumer acceptance test,
so the API removes the real duplication rather than only achieving method-name parity. Decide and
document the implicit-conversion question explicitly as part of that design.
