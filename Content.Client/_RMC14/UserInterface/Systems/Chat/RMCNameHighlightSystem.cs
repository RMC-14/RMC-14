using System.Linq;
using System.Text.RegularExpressions;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Xenonids.Name;

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
        args.Highlights = MarineNicknameRegex.Replace(args.Highlights, "@${FirstName}\n@${NickName}\n@${LastName}\n");
    }

    private void XenoNameHighlights(Entity<XenoNameComponent> ent, ref BeforeCharacterChatHighlightsUpdatedEvent args)
    {
        // Add the xeno's custom name and their number, if applicable.
        var prefix = _xenoName.GetXenoPrefix(ent.Owner);
        var newHighlights = $"@\"{prefix}{ent.Comp.Postfix}\"\n";

        if (!HasComp<XenoOmitNumberComponent>(ent))
            newHighlights += $"@\"{ent.Comp.Number}\"\n";

        // Inserted to the start of the highlights string since name stuff usually goes first.
        args.Highlights = args.Highlights.Insert(0, newHighlights);

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
