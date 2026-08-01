using Content.Client._RMC14.UserInterface.Systems.Chat;
using Content.Shared._RMC14.Xenonids.Name;
using System.Linq;

namespace Content.Client._RMC14.Xenonids.Name;

public sealed class XenoNameSystem : SharedXenoNameSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoNameComponent, BeforeCharacterChatHighlightsUpdatedEvent>(OnChatHighlightsUpdated);
    }

    private void OnChatHighlightsUpdated(Entity<XenoNameComponent> ent, ref BeforeCharacterChatHighlightsUpdatedEvent args)
    {
        // Add the xeno's custom name and their number, if applicable.
        var prefix = GetXenoPrefix(ent.Owner);
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
