using System.Runtime.CompilerServices;

namespace Reunion.Tests;

public sealed class CompilerContractTests
{
    [Fact]
    public void ExhaustiveSwitchesCoverEveryUnionFamily()
    {
        Assert.Equal("success", Match(Result.Success()));
        Assert.Equal("error", Match(Result.Failure("error")));
        Assert.Equal("42", Match(Result.Success<int>(42)));
        Assert.Equal("error", Match(Result.Failure<int>("error")));
        Assert.Equal("42", Match(Result.Success<int, string>(42)));
        Assert.Equal("error", Match(Result.Failure<int, string>("error")));
        Assert.Equal("success", Match(UnitResult.Success<string>()));
        Assert.Equal("error", Match(UnitResult.Failure("error")));
        Assert.Equal("42", Match(Option.Some(42)));
        Assert.Equal("none", Match(Option.None<int>()));
    }

    [Fact]
    public void NativeImplicitConversionsCoverEveryCase()
    {
        Result resultSuccess = new Success();
        Result resultFailure = new Failure<string>("error");
        Result<int> valueSuccess = new Success<int>(42);
        Result<int> valueFailure = new Failure<string>("error");
        Result<int, string> typedSuccess = new Success<int>(42);
        Result<int, string> typedFailure = new Failure<string>("error");
        UnitResult<string> unitSuccess = new Success();
        UnitResult<string> unitFailure = new Failure<string>("error");
        Option<int> some = new Some<int>(42);
        Option<int> none = new None();

        Assert.True(resultSuccess.IsSuccess);
        Assert.True(resultFailure.IsFailure);
        Assert.True(valueSuccess.IsSuccess);
        Assert.True(valueFailure.IsFailure);
        Assert.True(typedSuccess.IsSuccess);
        Assert.True(typedFailure.IsFailure);
        Assert.True(unitSuccess.IsSuccess);
        Assert.True(unitFailure.IsFailure);
        Assert.True(some.IsSome);
        Assert.True(none.IsNone);
    }

    [Fact]
    public void RawPayloadConversionsPreserveNamedUnionCases()
    {
        Result resultFailure = "error";
        Result<int> valueSuccess = 42;
        Result<int> valueFailure = "error";
        Result<int, string> typedSuccess = 42;
        Result<int, string> typedFailure = "error";
        UnitResult<string> unitFailure = "error";
        Option<int> some = 42;

        Assert.IsType<Failure<string>>(((IUnion)resultFailure).Value);
        Assert.IsType<Success<int>>(((IUnion)valueSuccess).Value);
        Assert.IsType<Failure<string>>(((IUnion)valueFailure).Value);
        Assert.IsType<Success<int>>(((IUnion)typedSuccess).Value);
        Assert.IsType<Failure<string>>(((IUnion)typedFailure).Value);
        Assert.IsType<Failure<string>>(((IUnion)unitFailure).Value);
        Assert.IsType<Some<int>>(((IUnion)some).Value);
    }

    [Fact]
    public void SamePayloadTypeRemainsDiscriminated()
    {
        Result<string, string> success = new Success<string>("same");
        Result<string, string> failure = new Failure<string>("same");

        Assert.Equal("success:same", MatchSameType(success));
        Assert.Equal("failure:same", MatchSameType(failure));
    }

    [Fact]
    public void ReferenceAndValueTypePayloadsAreSupported()
    {
        Result<string, int> referenceSuccess = new Success<string>("value");
        Result<string, int> valueFailure = new Failure<int>(42);
        Option<string> referenceSome = new Some<string>("value");
        Option<int> valueSome = new Some<int>(42);

        Assert.Equal("value", MatchReferenceAndValue(referenceSuccess));
        Assert.Equal("42", MatchReferenceAndValue(valueFailure));
        Assert.Equal("value", MatchReferenceOption(referenceSome));
        Assert.Equal("42", Match(valueSome));
    }

    [Fact]
    public void DefaultOptionMatchesNone()
    {
        Option<int> option = default;

        Assert.Equal("none", Match(option));
    }

    [Fact]
    public void DefaultPayloadWrappersCannotBypassFactoryValidation()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            Result<string> _ = default(Success<string>);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            Result<string, int> _ = default(Success<string>);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            Result<int, string> _ = default(Failure<string>);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            UnitResult<string> _ = default(Failure<string>);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            Option<string> _ = default(Some<string>);
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void StringFailureCasesShareWhitespaceValidation(string error)
    {
        Assert.Throws<ArgumentException>(() => new Failure<string>(error));
        Assert.Throws<ArgumentException>(() => Result.Failure<int, string>(error));
        Assert.Throws<ArgumentException>(() => UnitResult.Failure(error));
    }

    [Fact]
    public void ExistingTryGetValueOutVarRemainsUnambiguous()
    {
        var result = Result.Success<int, string>(42);
        var option = Option.Some(42);

        Assert.True(result.TryGetValue(out var resultValue));
        Assert.True(option.TryGetValue(out var optionValue));
        Assert.Equal(42, resultValue);
        Assert.Equal(42, optionValue);
    }

    [Fact]
    public void DefaultResultsMatchTheUnionNullState()
    {
        Assert.Equal("uninitialized", MatchDefault(default(Result)));
        Assert.Equal("uninitialized", MatchDefault(default(Result<int>)));
        Assert.Equal("uninitialized", MatchDefault(default(Result<int, string>)));
        Assert.Equal("uninitialized", MatchDefault(default(UnitResult<string>)));
    }

    [Fact]
    public void NamedCasesSupportPositionalPatterns()
    {
        Assert.Equal("42", MatchPositional(Result.Success<int, string>(42)));
        Assert.Equal("error", MatchPositional(Result.Failure<int, string>("error")));
    }

    [Fact]
    public void UnionAccessorsAreNotExposedDirectlyOnFunctionalTypes()
    {
        Assert.Empty(PublicTryGetValueMethods(typeof(Result)));
        Assert.Single(PublicTryGetValueMethods(typeof(Result<int>)));
        Assert.Single(PublicTryGetValueMethods(typeof(Result<int, string>)));
        Assert.Empty(PublicTryGetValueMethods(typeof(UnitResult<string>)));
        Assert.Single(PublicTryGetValueMethods(typeof(Option<int>)));
    }

    [Fact]
    public void IUnionValueReturnsDeclaredCasesOrNull()
    {
        Assert.IsType<Success>(((IUnion)Result.Success()).Value);
        Assert.IsType<Failure<string>>(((IUnion)Result.Failure("error")).Value);
        Assert.IsType<Success<int>>(((IUnion)Result.Success<int>(42)).Value);
        Assert.IsType<Failure<string>>(((IUnion)Result.Failure<int>("error")).Value);
        Assert.IsType<Success<int>>(((IUnion)Result.Success<int, string>(42)).Value);
        Assert.IsType<Failure<string>>(((IUnion)Result.Failure<int, string>("error")).Value);
        Assert.IsType<Success>(((IUnion)UnitResult.Success<string>()).Value);
        Assert.IsType<Failure<string>>(((IUnion)UnitResult.Failure("error")).Value);
        Assert.IsType<Some<int>>(((IUnion)Option.Some(42)).Value);
        Assert.IsType<None>(((IUnion)Option.None<int>()).Value);
        Assert.Null(((IUnion)default(Result)).Value);
        Assert.Null(((IUnion)default(Result<int>)).Value);
        Assert.Null(((IUnion)default(Result<int, string>)).Value);
        Assert.Null(((IUnion)default(UnitResult<string>)).Value);
    }

    [Fact]
    public void ExhaustiveMatchingUsesStronglyTypedAccessorsInsteadOfValue()
    {
        InstrumentedUnion.ValueReads = 0;
        InstrumentedUnion first = new FirstCase(42);
        InstrumentedUnion second = new SecondCase("value");

        Assert.Equal("42", MatchInstrumented(first));
        Assert.Equal("value", MatchInstrumented(second));
        Assert.Equal(0, InstrumentedUnion.ValueReads);
    }

    private static string Match(Result result) => result switch
    {
        Success _ => "success",
        Failure<string> failure => failure.Error
    };

    private static string Match(Result<int> result) => result switch
    {
        Success<int> success => success.Value.ToString(),
        Failure<string> failure => failure.Error
    };

    private static string Match(Result<int, string> result) => result switch
    {
        Success<int> success => success.Value.ToString(),
        Failure<string> failure => failure.Error
    };

    private static string Match(UnitResult<string> result) => result switch
    {
        Success _ => "success",
        Failure<string> failure => failure.Error
    };

    private static string Match(Option<int> option) => option switch
    {
        Some<int> some => some.Value.ToString(),
        None _ => "none"
    };

    private static string MatchSameType(Result<string, string> result) => result switch
    {
        Success<string> success => $"success:{success.Value}",
        Failure<string> failure => $"failure:{failure.Error}"
    };

    private static string MatchReferenceAndValue(Result<string, int> result) => result switch
    {
        Success<string> success => success.Value,
        Failure<int> failure => failure.Error.ToString()
    };

    private static string MatchReferenceOption(Option<string> option) => option switch
    {
        Some<string> some => some.Value,
        None _ => "none"
    };

    private static string MatchDefault(Result result) => result switch
    {
        null => "uninitialized",
        Success _ => "success",
        Failure<string> failure => failure.Error
    };

    private static string MatchDefault(Result<int> result) => result switch
    {
        null => "uninitialized",
        Success<int> success => success.Value.ToString(),
        Failure<string> failure => failure.Error
    };

    private static string MatchDefault(Result<int, string> result) => result switch
    {
        null => "uninitialized",
        Success<int> success => success.Value.ToString(),
        Failure<string> failure => failure.Error
    };

    private static string MatchDefault(UnitResult<string> result) => result switch
    {
        null => "uninitialized",
        Success _ => "success",
        Failure<string> failure => failure.Error
    };

    private static string MatchPositional(Result<int, string> result) => result switch
    {
        Success<int>(var value) => value.ToString(),
        Failure<string>(var error) => error
    };

    private static string MatchInstrumented(InstrumentedUnion union) => union switch
    {
        FirstCase first => first.Value.ToString(),
        SecondCase second => second.Value
    };

    private static System.Reflection.MethodInfo[] PublicTryGetValueMethods(Type type) =>
        type.GetMethods(
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.DeclaredOnly)
            .Where(method => method.Name == "TryGetValue")
            .ToArray();

    public readonly struct FirstCase(int value)
    {
        public int Value { get; } = value;
    }

    public readonly struct SecondCase(string value)
    {
        public string Value { get; } = value;
    }

    [Union]
    public readonly struct InstrumentedUnion : IUnion, InstrumentedUnion.IUnionMembers
    {
        private readonly byte tag;
        private readonly int first;
        private readonly string? second;

        private InstrumentedUnion(byte tag, int first, string? second)
        {
            this.tag = tag;
            this.first = first;
            this.second = second;
        }

        public static int ValueReads { get; set; }

        public interface IUnionMembers
        {
            public static InstrumentedUnion Create(FirstCase value) => new(1, value.Value, null);

            public static InstrumentedUnion Create(SecondCase value) => new(2, 0, value.Value);

            public object? Value { get; }

            public bool HasValue { get; }

            public bool TryGetValue(out FirstCase value);

            public bool TryGetValue(out SecondCase value);
        }

        object? IUnion.Value
        {
            get
            {
                ValueReads++;
                return this.tag switch
                {
                    1 => new FirstCase(this.first),
                    2 => new SecondCase(this.second!),
                    _ => null
                };
            }
        }

        object? IUnionMembers.Value => ((IUnion)this).Value;

        bool IUnionMembers.HasValue => this.tag is 1 or 2;

        bool IUnionMembers.TryGetValue(out FirstCase value)
        {
            value = new FirstCase(this.first);
            return this.tag == 1;
        }

        bool IUnionMembers.TryGetValue(out SecondCase value)
        {
            value = this.tag == 2 ? new SecondCase(this.second!) : default;
            return this.tag == 2;
        }
    }
}
