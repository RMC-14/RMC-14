using System.Text;
using Content.Shared._RMC14.Language.Systems;

namespace Content.Shared._RMC14.Language;

[ImplicitDataDefinitionForInheritors]
public abstract partial class ObfuscationMethod
{
    public static readonly ObfuscationMethod Default = new ReplacementObfuscation
    {
        Replacement = new List<string> { "<?>" }
    };

    [DataField]
    public float ComprehensionVariance = 0.1f;

    internal abstract void ObfuscateInternal(
        StringBuilder builder,
        string message,
        SharedLanguageSystem context,
        bool randomize);

    internal abstract void ObfuscateInternalWithComprehension(
        StringBuilder builder,
        string message,
        SharedLanguageSystem context,
        bool randomize,
        float comprehension);

    protected static bool IsPunctuation(char ch) => ch is '.' or '!' or '?' or ',' or ':';

    protected static bool IsSentenceEndPunctuation(char ch) => ch is '.' or '!' or '?';

    protected static float CalculateWordComprehension(
        string word,
        float baseComprehension,
        SharedLanguageSystem context,
        bool randomize,
        float varianceRange)
    {
        var random = context.CreateRandom(word.GetHashCode(), randomize);
        var variance = random.NextSingle() * (varianceRange * 2f) - varianceRange;
        return Math.Clamp(baseComprehension + variance, 0.0f, 1.0f);
    }
}
