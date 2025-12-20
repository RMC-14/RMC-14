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
        bool randomize)
    {
        var random = context.CreateRandom(word.GetHashCode(), randomize);
        var variance = random.NextSingle() * 0.2f - 0.1f;
        return Math.Clamp(baseComprehension + variance, 0.0f, 1.0f);
    }
}
