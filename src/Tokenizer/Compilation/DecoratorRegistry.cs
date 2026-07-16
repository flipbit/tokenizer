using Tokens.Transformers;
using Tokens.Validators;

namespace Tokens.Compilation;

/// <summary>
/// Provides built-in and user-registered decorator registrations.
/// Built-in decorators use an explicit static dictionary instead of
/// assembly scanning, making the library compatible with Native AOT.
/// </summary>
internal sealed class DecoratorRegistry
{
#pragma warning disable CS0618 // ToDateTimeUtcTransformer is intentionally included for backward compatibility
    private static readonly Dictionary<Type, Func<ITokenDecorator>> BuiltInDecorators = new()
    {
        // Transformers
        [typeof(DefaultValueTransformer)] = () => new DefaultValueTransformer(),
        [typeof(RegexReplaceTransformer)] = () => new RegexReplaceTransformer(),
        [typeof(RemoveEndTransformer)] = () => new RemoveEndTransformer(),
        [typeof(RemoveStartTransformer)] = () => new RemoveStartTransformer(),
        [typeof(RemoveTransformer)] = () => new RemoveTransformer(),
        [typeof(ReplaceTransformer)] = () => new ReplaceTransformer(),
        [typeof(SetTransformer)] = () => new SetTransformer(),
        [typeof(SplitTransformer)] = () => new SplitTransformer(),
        [typeof(SubstringAfterLastTransformer)] = () => new SubstringAfterLastTransformer(),
        [typeof(SubstringAfterTransformer)] = () => new SubstringAfterTransformer(),
        [typeof(SubstringBeforeLastTransformer)] = () => new SubstringBeforeLastTransformer(),
        [typeof(SubstringBeforeTransformer)] = () => new SubstringBeforeTransformer(),
        [typeof(TitleCaseTransformer)] = () => new TitleCaseTransformer(),
        [typeof(ToBooleanTransformer)] = () => new ToBooleanTransformer(),
        [typeof(ToDateTimeTransformer)] = () => new ToDateTimeTransformer(),
        [typeof(ToDateTimeUtcTransformer)] = () => new ToDateTimeUtcTransformer(),
        [typeof(ToDecimalTransformer)] = () => new ToDecimalTransformer(),
        [typeof(ToGuidTransformer)] = () => new ToGuidTransformer(),
        [typeof(ToIntTransformer)] = () => new ToIntTransformer(),
        [typeof(ToLowerTransformer)] = () => new ToLowerTransformer(),
        [typeof(ToUpperTransformer)] = () => new ToUpperTransformer(),
        [typeof(TrimTransformer)] = () => new TrimTransformer(),
        [typeof(TruncateTransformer)] = () => new TruncateTransformer(),

        // Validators
        [typeof(ContainsValidator)] = () => new ContainsValidator(),
        [typeof(EndsWithValidator)] = () => new EndsWithValidator(),
        [typeof(IsAlphanumericValidator)] = () => new IsAlphanumericValidator(),
        [typeof(IsDateTimeValidator)] = () => new IsDateTimeValidator(),
        [typeof(IsDomainNameValidator)] = () => new IsDomainNameValidator(),
        [typeof(IsEmailValidator)] = () => new IsEmailValidator(),
        [typeof(IsGuidValidator)] = () => new IsGuidValidator(),
        [typeof(IsInRangeValidator)] = () => new IsInRangeValidator(),
        [typeof(IsIntegerValidator)] = () => new IsIntegerValidator(),
        [typeof(IsIpAddressValidator)] = () => new IsIpAddressValidator(),
        [typeof(IsLooseAbsoluteUrlValidator)] = () => new IsLooseAbsoluteUrlValidator(),
        [typeof(IsLooseUrlValidator)] = () => new IsLooseUrlValidator(),
        [typeof(IsNotEmptyValidator)] = () => new IsNotEmptyValidator(),
        [typeof(IsNotValidator)] = () => new IsNotValidator(),
        [typeof(IsNumericValidator)] = () => new IsNumericValidator(),
        [typeof(IsPhoneNumberValidator)] = () => new IsPhoneNumberValidator(),
        [typeof(IsUrlValidator)] = () => new IsUrlValidator(),
        [typeof(MatchesRegexValidator)] = () => new MatchesRegexValidator(),
        [typeof(MaxLengthValidator)] = () => new MaxLengthValidator(),
        [typeof(MinLengthValidator)] = () => new MinLengthValidator(),
        [typeof(StartsWithValidator)] = () => new StartsWithValidator(),

#if NET6_0_OR_GREATER
        [typeof(ToDateTransformer)] = () => new ToDateTransformer(),
        [typeof(ToTimeTransformer)] = () => new ToTimeTransformer(),
        [typeof(IsDateValidator)] = () => new IsDateValidator(),
        [typeof(IsTimeValidator)] = () => new IsTimeValidator(),
#endif
    };
#pragma warning restore CS0618

    public IReadOnlyList<DecoratorRegistration> Transformers { get; }

    public IReadOnlyList<DecoratorRegistration> Validators { get; }

    public DecoratorRegistry(TokenizerOptions options)
    {
        var transformers = new List<DecoratorRegistration>();
        var validators = new List<DecoratorRegistration>();

        foreach (var kvp in BuiltInDecorators)
        {
            if (typeof(ITokenTransformer).IsAssignableFrom(kvp.Key))
            {
                transformers.Add(new DecoratorRegistration(kvp.Key, kvp.Value));
            }

            if (typeof(ITokenValidator).IsAssignableFrom(kvp.Key))
            {
                validators.Add(new DecoratorRegistration(kvp.Key, kvp.Value));
            }
        }

        foreach (var reg in options.TransformerRegistrations)
        {
            if (!transformers.Exists(r => r.Type == reg.Type))
            {
                transformers.Add(reg);
            }
        }

        foreach (var reg in options.ValidatorRegistrations)
        {
            if (!validators.Exists(r => r.Type == reg.Type))
            {
                validators.Add(reg);
            }
        }

        Transformers = transformers.AsReadOnly();
        Validators = validators.AsReadOnly();
    }
}
