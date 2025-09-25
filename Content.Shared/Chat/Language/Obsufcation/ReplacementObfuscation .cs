using System.Text;
using Content.Shared._RMC14.Language.Systems;

namespace Content.Shared._RMC14.Language;

public partial class ReplacementObfuscation : ObfuscationMethod
{
    [DataField(required: true)]
    public List<string> Replacement = [];

    internal override void ObfuscateInternal(StringBuilder builder, string message, SharedLanguageSystem context)
    {
        ObfuscateInternalWithComprehension(builder, message, context, 0.0f);
    }

    internal override void ObfuscateInternalWithComprehension(
        StringBuilder builder,
        string message,
        SharedLanguageSystem context,
        float comprehension)
    {
        if (Replacement.Count == 0) return;

        var index = context.PseudoRandomNumber(message.GetHashCode(), 0, Replacement.Count - 1);
        builder.Append(Replacement[index]);
    }
}
