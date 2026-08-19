using System.Text;
using Content.Shared._RMC14.Language.Systems;

namespace Content.Shared._RMC14.Language;

[Virtual]
public partial class ReplacementObfuscation : ObfuscationMethod
{
    [DataField(required: true)]
    public List<string> Replacement = [];

    internal override void ObfuscateInternal(
        StringBuilder builder,
        string message,
        SharedLanguageSystem context,
        bool randomize)
    {
        ObfuscateInternalWithComprehension(builder, message, context, randomize, 0.0f);
    }

    internal override void ObfuscateInternalWithComprehension(
        StringBuilder builder,
        string message,
        SharedLanguageSystem context,
        bool randomize,
        float comprehension)
    {
        if (Replacement.Count == 0)
            return;

        var wordBeginIndex = 0;
        var hashCode = 0;

        for (var i = 0; i <= message.Length; i++)
        {
            var ch = i < message.Length ? char.ToLowerInvariant(message[i]) : '\0';
            var isWordEnd = char.IsWhiteSpace(ch) || IsPunctuation(ch) || ch == '\0';

            if (!isWordEnd)
            {
                hashCode = hashCode * 31 + ch;
                continue;
            }

            var wordLength = i - wordBeginIndex;
            if (wordLength > 0)
            {
                var index = context.PseudoRandomNumber(hashCode, 0, Replacement.Count - 1, randomize);
                builder.Append(Replacement[index]);
            }

            if (isWordEnd && ch != '\0')
                builder.Append(message[i]);

            hashCode = 0;
            wordBeginIndex = i + 1;
        }
    }
}
