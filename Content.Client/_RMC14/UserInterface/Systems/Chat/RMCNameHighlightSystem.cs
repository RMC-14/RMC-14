using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Xenonids.Name;
using System.Linq;
using System.Text.RegularExpressions;

namespace Content.Client._RMC14.UserInterface.Systems.Chat;

public sealed partial class RMCNameHighlightSystem : EntitySystem
{
    [Dependency] private readonly SharedXenoNameSystem _xenoName = default!;

#pragma warning disable SYSLIB1045 // `GeneratedRegexAttribute` isn't allowed by client sandboxing.
    private static readonly Regex MarineNicknameRegex = new(
        """^@(?<FirstName>.+) ['"](?<NickName>.+?)['"] (?<LastName>.+)\n""",
        RegexOptions.Compiled);
#pragma warning restore SYSLIB1045

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MarineComponent, BeforeCharacterChatHighlightsUpdatedEvent>(MarineNameHighlights);
        SubscribeLocalEvent<XenoNameComponent, BeforeCharacterChatHighlightsUpdatedEvent>(XenoNameHighlights);
    }

    private void MarineNameHighlights(Entity<MarineComponent> ent, ref BeforeCharacterChatHighlightsUpdatedEvent args)
    {
        // If their name matches the {FirstName 'NickName' LastName} pattern, split each into its own highlight line.
        args.Highlights = MarineNicknameRegex.Replace(args.Highlights, "@\"${FirstName}\"\n@\"${NickName}\"\n@\"${LastName}\"\n");
    }

    private void XenoNameHighlights(Entity<XenoNameComponent> ent, ref BeforeCharacterChatHighlightsUpdatedEvent args)
    {
        // Add their number if applicable. (excludes the queen)
        if (!HasComp<XenoOmitNumberComponent>(ent))
            // Inserted to the start of the highlights string since name stuff usually goes first.
            args.Highlights = args.Highlights.Insert(0, $"@\"{ent.Comp.Number}\"\n");

        // Add their combined prefix + postfix above that.
        var prefix = _xenoName.GetXenoPrefix(ent.Owner); // Either their custom prefix or 'XX'.
        var postfix = ent.Comp.Postfix; // Either their custom postfix or `null`.
        args.Highlights = args.Highlights.Insert(0, $"@\"{prefix}{postfix}\"\n");

        // Remove the full xeno name since people won't tend to say "Young Drone (XX-123)" when talking to you.
        args.Highlights = args.Highlights.Replace($"@{args.Data.EntityName}\n", "");

        // Remove any badly formatted highlights from the system mistaking the xeno name for a lizard name.
        if (args.Data.EntityName.Count(c => c == '-') > 1)
        {
            var hyphenSplit = args.Data.EntityName.Split('-');
            args.Highlights = args.Highlights.Replace($"@{hyphenSplit[0]}\n@{hyphenSplit[^1]}\n", "");
        }
    }
}
