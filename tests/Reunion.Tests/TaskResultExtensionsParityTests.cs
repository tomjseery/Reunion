using Reunion;

namespace Reunion.Tests;

public sealed class TaskResultExtensionsParityTests
{
    [Fact]
    public async Task TaskSourceSynchronousBindOverloads_CoverEveryInstanceTarget()
    {
        var invocations = 0;

        Assert.Equal(
            Result.Success(1),
            await Task.FromResult(Result.Success()).Bind(() =>
            {
                invocations++;
                return Result.Success(1);
            }));
        Assert.Equal(
            Result.Failure<int>("status"),
            await Task.FromResult(Result.Failure("status")).Bind(() =>
            {
                invocations++;
                return Result.Success(1);
            }));

        var typedSuccess = Task.FromResult(Result.Success<int, int>(1));
        var typedFailure = Task.FromResult(Result.Failure<int, int>(2));
        Assert.Equal(Result.Success(), await typedSuccess.Bind(
            _ =>
            {
                invocations++;
                return Result.Success();
            },
            error => error.ToString()));
        Assert.Equal(Result.Failure("2"), await typedFailure.Bind(
            _ =>
            {
                invocations++;
                return Result.Success();
            },
            error => error.ToString()));
        Assert.Equal(Result.Success("value"), await typedSuccess.Bind(
            _ =>
            {
                invocations++;
                return Result.Success("value");
            },
            error => error.ToString()));
        Assert.Equal(Result.Failure<string>("2"), await typedFailure.Bind(
            _ =>
            {
                invocations++;
                return Result.Success("value");
            },
            error => error.ToString()));

        var unitSuccess = Task.FromResult(UnitResult.Success<int>());
        var unitFailure = Task.FromResult(UnitResult.Failure(3));
        Assert.Equal(Result.Success(), await unitSuccess.Bind(
            () =>
            {
                invocations++;
                return Result.Success();
            },
            error => error.ToString()));
        Assert.Equal(Result.Failure("3"), await unitFailure.Bind(
            () =>
            {
                invocations++;
                return Result.Success();
            },
            error => error.ToString()));
        Assert.Equal(Result.Success("value"), await unitSuccess.Bind(
            () =>
            {
                invocations++;
                return Result.Success("value");
            },
            error => error.ToString()));
        Assert.Equal(Result.Failure<string>("3"), await unitFailure.Bind(
            () =>
            {
                invocations++;
                return Result.Success("value");
            },
            error => error.ToString()));

        Assert.Equal(5, invocations);
    }

    [Fact]
    public async Task TaskSourceMappedBindOverloads_InferContinuationErrorType()
    {
        var invocations = 0;
        var unitSuccess = Task.FromResult(UnitResult.Success<int>());
        var unitFailure = Task.FromResult(UnitResult.Failure(2));
        var valueSuccess = Task.FromResult(Result.Success<int, int>(3));
        var valueFailure = Task.FromResult(Result.Failure<int, int>(4));

        Assert.Equal(
            UnitResult.Success<ApplicationError>(),
            await unitSuccess.Bind(UnitContinuation, error => new MappedError(error)));
        Assert.Equal(
            UnitResult.Failure<ApplicationError>(new MappedError(2)),
            await unitFailure.Bind(UnitContinuation, error => new MappedError(error)));
        Assert.Equal(
            Result.Success<int, ApplicationError>(42),
            await unitSuccess.Bind(ValueContinuation, error => new MappedError(error)));
        Assert.Equal(
            Result.Failure<int, ApplicationError>(new MappedError(2)),
            await unitFailure.Bind(ValueContinuation, error => new MappedError(error)));
        Assert.Equal(
            UnitResult.Success<ApplicationError>(),
            await valueSuccess.Bind(_ => UnitContinuation(), error => new MappedError(error)));
        Assert.Equal(
            UnitResult.Failure<ApplicationError>(new MappedError(4)),
            await valueFailure.Bind(_ => UnitContinuation(), error => new MappedError(error)));
        Assert.Equal(
            Result.Success<int, ApplicationError>(42),
            await valueSuccess.Bind(_ => ValueContinuation(), error => new MappedError(error)));
        Assert.Equal(
            Result.Failure<int, ApplicationError>(new MappedError(4)),
            await valueFailure.Bind(_ => ValueContinuation(), error => new MappedError(error)));
        Assert.Equal(4, invocations);

        UnitResult<ApplicationError> UnitContinuation()
        {
            invocations++;
            return UnitResult.Success<ApplicationError>();
        }

        Result<int, ApplicationError> ValueContinuation()
        {
            invocations++;
            return Result.Success<int, ApplicationError>(42);
        }
    }

    [Fact]
    public async Task ResultBindAsyncOverloads_BareAndTaskReceivers_PreserveCasesAndMapErrors()
    {
        var invocations = 0;
        Func<Task<Result<int>>> valueBind = () =>
        {
            invocations++;
            return Task.FromResult(Result.Success(1));
        };
        Func<Task<UnitResult<int>>> unitBind = () =>
        {
            invocations++;
            return Task.FromResult(UnitResult.Success<int>());
        };
        Func<Task<Result<int, int>>> typedBind = () =>
        {
            invocations++;
            return Task.FromResult(Result.Success<int, int>(1));
        };

        Assert.Equal(Result.Success(1), await Result.Success().BindAsync(valueBind));
        Assert.Equal(Result.Failure<int>("error"), await Result.Failure("error").BindAsync(valueBind));
        Assert.Equal(Result.Success(1), await Task.FromResult(Result.Success()).BindAsync(valueBind));
        Assert.Equal(Result.Failure<int>("error"), await Task.FromResult(Result.Failure("error")).BindAsync(valueBind));

        Assert.Equal(UnitResult.Success<int>(), await Result.Success().BindAsync(unitBind, error => error.Length));
        Assert.Equal(UnitResult.Failure(5), await Result.Failure("error").BindAsync(unitBind, error => error.Length));
        Assert.Equal(UnitResult.Success<int>(), await Task.FromResult(Result.Success()).BindAsync(unitBind, error => error.Length));
        Assert.Equal(UnitResult.Failure(5), await Task.FromResult(Result.Failure("error")).BindAsync(unitBind, error => error.Length));

        Assert.Equal(Result.Success<int, int>(1), await Result.Success().BindAsync(typedBind, error => error.Length));
        Assert.Equal(Result.Failure<int, int>(5), await Result.Failure("error").BindAsync(typedBind, error => error.Length));
        Assert.Equal(Result.Success<int, int>(1), await Task.FromResult(Result.Success()).BindAsync(typedBind, error => error.Length));
        Assert.Equal(Result.Failure<int, int>(5), await Task.FromResult(Result.Failure("error")).BindAsync(typedBind, error => error.Length));

        Assert.Equal(6, invocations);
    }

    [Fact]
    public async Task ValueAndTypedResultBindAsyncOverloads_BareAndTaskReceivers_PreserveCases()
    {
        var invocations = 0;
        Func<int, Task<Result>> valueToStatus = _ =>
        {
            invocations++;
            return Task.FromResult(Result.Success());
        };
        var valueSuccess = Result.Success(1);
        var valueFailure = Result.Failure<int>("error");

        Assert.Equal(Result.Success(), await valueSuccess.BindAsync(valueToStatus));
        Assert.Equal(Result.Failure("error"), await valueFailure.BindAsync(valueToStatus));
        Assert.Equal(Result.Success(), await Task.FromResult(valueSuccess).BindAsync(valueToStatus));
        Assert.Equal(Result.Failure("error"), await Task.FromResult(valueFailure).BindAsync(valueToStatus));

        Func<int, Task<Result>> typedToStatus = _ =>
        {
            invocations++;
            return Task.FromResult(Result.Success());
        };
        Func<int, Task<Result<string>>> typedToValue = value =>
        {
            invocations++;
            return Task.FromResult(Result.Success(value.ToString()));
        };
        var typedSuccess = Result.Success<int, int>(1);
        var typedFailure = Result.Failure<int, int>(2);

        Assert.Equal(Result.Success(), await typedSuccess.BindAsync(typedToStatus, error => error.ToString()));
        Assert.Equal(Result.Failure("2"), await typedFailure.BindAsync(typedToStatus, error => error.ToString()));
        Assert.Equal(Result.Success(), await Task.FromResult(typedSuccess).BindAsync(typedToStatus, error => error.ToString()));
        Assert.Equal(Result.Failure("2"), await Task.FromResult(typedFailure).BindAsync(typedToStatus, error => error.ToString()));
        Assert.Equal(Result.Success("1"), await typedSuccess.BindAsync(typedToValue, error => error.ToString()));
        Assert.Equal(Result.Failure<string>("2"), await typedFailure.BindAsync(typedToValue, error => error.ToString()));
        Assert.Equal(Result.Success("1"), await Task.FromResult(typedSuccess).BindAsync(typedToValue, error => error.ToString()));
        Assert.Equal(Result.Failure<string>("2"), await Task.FromResult(typedFailure).BindAsync(typedToValue, error => error.ToString()));

        Assert.Equal(6, invocations);
    }

    [Fact]
    public async Task UnitResultBindAsyncOverloads_BareAndTaskReceivers_PreserveCasesAndMapErrors()
    {
        var invocations = 0;
        Func<Task<Result>> statusBind = () =>
        {
            invocations++;
            return Task.FromResult(Result.Success());
        };
        Func<Task<Result<string>>> valueBind = () =>
        {
            invocations++;
            return Task.FromResult(Result.Success("value"));
        };
        var success = UnitResult.Success<int>();
        var failure = UnitResult.Failure(2);

        Assert.Equal(Result.Success(), await success.BindAsync(statusBind, error => error.ToString()));
        Assert.Equal(Result.Failure("2"), await failure.BindAsync(statusBind, error => error.ToString()));
        Assert.Equal(Result.Success(), await Task.FromResult(success).BindAsync(statusBind, error => error.ToString()));
        Assert.Equal(Result.Failure("2"), await Task.FromResult(failure).BindAsync(statusBind, error => error.ToString()));
        Assert.Equal(Result.Success("value"), await success.BindAsync(valueBind, error => error.ToString()));
        Assert.Equal(Result.Failure<string>("2"), await failure.BindAsync(valueBind, error => error.ToString()));
        Assert.Equal(Result.Success("value"), await Task.FromResult(success).BindAsync(valueBind, error => error.ToString()));
        Assert.Equal(Result.Failure<string>("2"), await Task.FromResult(failure).BindAsync(valueBind, error => error.ToString()));

        Assert.Equal(4, invocations);
    }

    [Fact]
    public async Task TypedMappedBindAsync_InfersContinuationErrorForBareAndTaskReceivers()
    {
        var invocations = 0;
        var unitSuccess = UnitResult.Success<int>();
        var unitFailure = UnitResult.Failure(2);
        var valueSuccess = Result.Success<int, int>(3);
        var valueFailure = Result.Failure<int, int>(4);

        Assert.Equal(
            UnitResult.Success<ApplicationError>(),
            await unitSuccess.BindAsync(UnitContinuation, error => new MappedError(error)));
        Assert.Equal(
            UnitResult.Failure<ApplicationError>(new MappedError(2)),
            await unitFailure.BindAsync(UnitContinuation, error => new MappedError(error)));
        Assert.Equal(
            Result.Success<int, ApplicationError>(42),
            await Task.FromResult(unitSuccess)
                .BindAsync(ValueContinuation, error => new MappedError(error)));
        Assert.Equal(
            Result.Failure<int, ApplicationError>(new MappedError(2)),
            await Task.FromResult(unitFailure)
                .BindAsync(ValueContinuation, error => new MappedError(error)));
        Assert.Equal(
            UnitResult.Success<ApplicationError>(),
            await valueSuccess.BindAsync(_ => UnitContinuation(), error => new MappedError(error)));
        Assert.Equal(
            UnitResult.Failure<ApplicationError>(new MappedError(4)),
            await valueFailure.BindAsync(_ => UnitContinuation(), error => new MappedError(error)));
        Assert.Equal(
            Result.Success<int, ApplicationError>(42),
            await Task.FromResult(valueSuccess)
                .BindAsync(_ => ValueContinuation(), error => new MappedError(error)));
        Assert.Equal(
            Result.Failure<int, ApplicationError>(new MappedError(4)),
            await Task.FromResult(valueFailure)
                .BindAsync(_ => ValueContinuation(), error => new MappedError(error)));
        Assert.Equal(4, invocations);

        Task<UnitResult<ApplicationError>> UnitContinuation()
        {
            invocations++;
            return Task.FromResult(UnitResult.Success<ApplicationError>());
        }

        Task<Result<int, ApplicationError>> ValueContinuation()
        {
            invocations++;
            return Task.FromResult(Result.Success<int, ApplicationError>(42));
        }
    }

    [Fact]
    public async Task MapErrorAsync_AllFamilies_BareAndTaskReceivers_InvokeOnlyFailureOnce()
    {
        var invocations = 0;
        Task<int> MapString(string error)
        {
            invocations++;
            return Task.FromResult(error.Length);
        }
        Task<string> MapInt(int error)
        {
            invocations++;
            return Task.FromResult(error.ToString());
        }

        Assert.Equal(UnitResult.Success<int>(), await Result.Success().MapErrorAsync(MapString));
        Assert.Equal(UnitResult.Failure(5), await Result.Failure("error").MapErrorAsync(MapString));
        Assert.Equal(UnitResult.Success<int>(), await Task.FromResult(Result.Success()).MapErrorAsync(MapString));
        Assert.Equal(UnitResult.Failure(5), await Task.FromResult(Result.Failure("error")).MapErrorAsync(MapString));

        Assert.Equal(Result.Success<int, int>(1), await Result.Success(1).MapErrorAsync(MapString));
        Assert.Equal(Result.Failure<int, int>(5), await Result.Failure<int>("error").MapErrorAsync(MapString));
        Assert.Equal(Result.Success<int, int>(1), await Task.FromResult(Result.Success(1)).MapErrorAsync(MapString));
        Assert.Equal(Result.Failure<int, int>(5), await Task.FromResult(Result.Failure<int>("error")).MapErrorAsync(MapString));

        Assert.Equal(Result.Success<int, string>(1), await Result.Success<int, int>(1).MapErrorAsync(MapInt));
        Assert.Equal(Result.Failure<int, string>("2"), await Result.Failure<int, int>(2).MapErrorAsync(MapInt));
        Assert.Equal(Result.Success<int, string>(1), await Task.FromResult(Result.Success<int, int>(1)).MapErrorAsync(MapInt));
        Assert.Equal(Result.Failure<int, string>("2"), await Task.FromResult(Result.Failure<int, int>(2)).MapErrorAsync(MapInt));

        Assert.Equal(UnitResult.Success<string>(), await UnitResult.Success<int>().MapErrorAsync(MapInt));
        Assert.Equal(UnitResult.Failure("2"), await UnitResult.Failure(2).MapErrorAsync(MapInt));
        Assert.Equal(UnitResult.Success<string>(), await Task.FromResult(UnitResult.Success<int>()).MapErrorAsync(MapInt));
        Assert.Equal(UnitResult.Failure("2"), await Task.FromResult(UnitResult.Failure(2)).MapErrorAsync(MapInt));

        Assert.Equal(8, invocations);
    }

    [Fact]
    public async Task RecoverAsync_AllFamilies_BareAndTaskReceivers_InvokeOnlyFailureOnce()
    {
        var invocations = 0;
        Task RecoverStatus(string _)
        {
            invocations++;
            return Task.CompletedTask;
        }
        Task<int> RecoverValue(string error)
        {
            invocations++;
            return Task.FromResult(error.Length);
        }
        Task<int> RecoverTyped(int error)
        {
            invocations++;
            return Task.FromResult(error + 1);
        }
        Task RecoverUnit(int _)
        {
            invocations++;
            return Task.CompletedTask;
        }

        Assert.Equal(Result.Success(), await Result.Success().RecoverAsync(RecoverStatus));
        Assert.Equal(Result.Success(), await Result.Failure("error").RecoverAsync(RecoverStatus));
        Assert.Equal(Result.Success(), await Task.FromResult(Result.Success()).RecoverAsync(RecoverStatus));
        Assert.Equal(Result.Success(), await Task.FromResult(Result.Failure("error")).RecoverAsync(RecoverStatus));

        Assert.Equal(Result.Success(1), await Result.Success(1).RecoverAsync(RecoverValue));
        Assert.Equal(Result.Success(5), await Result.Failure<int>("error").RecoverAsync(RecoverValue));
        Assert.Equal(Result.Success(1), await Task.FromResult(Result.Success(1)).RecoverAsync(RecoverValue));
        Assert.Equal(Result.Success(5), await Task.FromResult(Result.Failure<int>("error")).RecoverAsync(RecoverValue));

        Assert.Equal(Result.Success<int, int>(1), await Result.Success<int, int>(1).RecoverAsync(RecoverTyped));
        Assert.Equal(Result.Success<int, int>(3), await Result.Failure<int, int>(2).RecoverAsync(RecoverTyped));
        Assert.Equal(Result.Success<int, int>(1), await Task.FromResult(Result.Success<int, int>(1)).RecoverAsync(RecoverTyped));
        Assert.Equal(Result.Success<int, int>(3), await Task.FromResult(Result.Failure<int, int>(2)).RecoverAsync(RecoverTyped));

        Assert.Equal(UnitResult.Success<int>(), await UnitResult.Success<int>().RecoverAsync(RecoverUnit));
        Assert.Equal(UnitResult.Success<int>(), await UnitResult.Failure(2).RecoverAsync(RecoverUnit));
        Assert.Equal(UnitResult.Success<int>(), await Task.FromResult(UnitResult.Success<int>()).RecoverAsync(RecoverUnit));
        Assert.Equal(UnitResult.Success<int>(), await Task.FromResult(UnitResult.Failure(2)).RecoverAsync(RecoverUnit));

        Assert.Equal(8, invocations);
    }

    [Fact]
    public async Task NewAsyncOverloads_ValidateNullsDefaultsAndBoundResults()
    {
        Task<Result> nullStatusSource = null!;
        Task<Result<int>> nullValueSource = null!;
        Task<Result<int, string>> nullTypedSource = null!;
        Task<UnitResult<string>> nullUnitSource = null!;

        await Assert.ThrowsAsync<ArgumentNullException>(() => nullStatusSource.Bind(() => Result.Success(1)));
        await Assert.ThrowsAsync<ArgumentNullException>(() => nullValueSource.BindAsync(_ => Task.FromResult(Result.Success())));
        await Assert.ThrowsAsync<ArgumentNullException>(() => nullTypedSource.MapErrorAsync(Task.FromResult));
        await Assert.ThrowsAsync<ArgumentNullException>(() => nullUnitSource.RecoverAsync(_ => Task.CompletedTask));

        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = Result.Success().BindAsync<int>(null!);
        });
        await Assert.ThrowsAsync<ArgumentNullException>(() => Result.Success(1).MapErrorAsync<int, string>(null!));
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = Result.Success<int, string>(1).RecoverAsync(null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = UnitResult.Success<string>().BindAsync(
                (Func<Task<Result>>)null!,
                error => error);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = Result.Success<int, string>(1).BindAsync(
                _ => Task.FromResult(Result.Success()),
                null!);
        });

        await Assert.ThrowsAsync<ArgumentNullException>(() => Result.Success().BindAsync<int>(() => null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => Result.Failure("error").MapErrorAsync<int>(_ => null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => Result.Failure("error").RecoverAsync(_ => null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => Result.Success(1).BindAsync(_ => (Task<Result>)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => Result.Failure<int>("error").MapErrorAsync<int, string>(_ => null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => Result.Failure<int>("error").RecoverAsync(_ => null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => Result.Success<int, string>(1).BindAsync(
            _ => (Task<Result>)null!,
            error => error));
        await Assert.ThrowsAsync<ArgumentNullException>(() => Result.Failure<int, string>("error").MapErrorAsync(_ => (Task<int>)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => Result.Failure<int, string>("error").RecoverAsync(_ => null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => UnitResult.Success<string>().BindAsync(
            () => (Task<Result>)null!,
            error => error));
        await Assert.ThrowsAsync<ArgumentNullException>(() => UnitResult.Failure("error").MapErrorAsync(_ => (Task<int>)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => UnitResult.Failure("error").RecoverAsync(_ => null!));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Result.Success().BindAsync(() => Task.FromResult(default(Result<int>))));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Result.Success<int, string>(1).BindAsync(
                _ => Task.FromResult(default(Result)),
                error => error));

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = default(Result).MapErrorAsync(_ => Task.FromResult(1));
        });
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            default(Result<int>).RecoverAsync(_ => Task.FromResult(1)));
        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = default(Result<int, string>).BindAsync(
                _ => Task.FromResult(Result.Success()),
                error => error);
        });
        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = default(UnitResult<string>).MapErrorAsync(_ => Task.FromResult(1));
        });
    }

    [Fact]
    public async Task NewAsyncOverloads_PropagateFaultsAndCancellation()
    {
        var expected = new TestException();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Same(expected, await Assert.ThrowsAsync<TestException>(() =>
            Task.FromException<Result>(expected).Bind(() => Result.Success(1))));
        Assert.Same(expected, await Assert.ThrowsAsync<TestException>(() =>
            Task.FromException<Result<int>>(expected).BindAsync(_ => Task.FromResult(Result.Success()))));
        Assert.Same(expected, await Assert.ThrowsAsync<TestException>(() =>
            Task.FromException<Result<int, string>>(expected).MapErrorAsync(Task.FromResult)));

        var cancelledSource = Task.FromCanceled<UnitResult<string>>(cancellation.Token)
            .RecoverAsync(_ => Task.CompletedTask);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelledSource);
        Assert.True(cancelledSource.IsCanceled);

        Assert.Same(expected, await Assert.ThrowsAsync<TestException>(() =>
            Result.Success().BindAsync<int>(() => Task.FromException<Result<int>>(expected))));
        Assert.Same(expected, await Assert.ThrowsAsync<TestException>(() =>
            Result.Failure("error").MapErrorAsync<int>(_ => Task.FromException<int>(expected))));
        Assert.Same(expected, await Assert.ThrowsAsync<TestException>(() =>
            Result.Failure<int, string>("error").RecoverAsync(_ => Task.FromException<int>(expected))));

        var bind = UnitResult.Success<string>().BindAsync(
            () => Task.FromCanceled<Result>(cancellation.Token),
            error => error);
        var map = Result.Failure<int>("error").MapErrorAsync<int, int>(
            _ => Task.FromCanceled<int>(cancellation.Token));
        var recover = UnitResult.Failure("error").RecoverAsync(
            _ => Task.FromCanceled(cancellation.Token));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => bind);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => map);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => recover);
        Assert.True(bind.IsCanceled);
        Assert.True(map.IsCanceled);
        Assert.True(recover.IsCanceled);
    }

    [Fact]
    public async Task SameTypeValueAndError_NewOverloads_AreUnambiguous()
    {
        var success = Result.Success<string, string>("value");
        var failure = Result.Failure<string, string>("error");

        Assert.Equal(
            Result.Success("value!"),
            await Task.FromResult(success).Bind(
                value => Result.Success(value + "!"),
                error => error));
        Assert.Equal(
            Result.Failure<string>("error"),
            await failure.BindAsync(
                value => Task.FromResult(Result.Success(value + "!")),
                error => error));
        Assert.Equal(
            Result.Failure<string, string>("error!"),
            await Task.FromResult(failure).MapErrorAsync(
                error => Task.FromResult(error + "!")));
        Assert.Equal(
            Result.Success<string, string>("error!"),
            await failure.RecoverAsync(error => Task.FromResult(error + "!")));
    }

    private sealed class TestException : Exception;

    private abstract record ApplicationError;

    private sealed record MappedError(int Value) : ApplicationError;
}
