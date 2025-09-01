using Robust.Shared.Utility;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Verbs;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;

namespace Content.Shared.RMCLoreExaminable;

public sealed class DetailExaminableSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCLoreExaminableComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
    }

    private void OnGetExamineVerbs(Entity<RMCLoreExaminableComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (HasComp<XenoComponent>(args.User))
            return;

        if (Identity.Name(args.Target, EntityManager) != MetaData(args.Target).EntityName)
            return;

        if (ent.Comp.Factions != null && ent.Comp.Factions.Count > 0 &&
            !_npcFaction.IsMemberOfAny(args.User, ent.Comp.Factions))
            return;

        var detailsRange = _examine.IsInDetailsRange(args.User, ent);

        var user = args.User;

        var verb = new ExamineVerb
        {
            Act = () =>
            {
                var markup = new FormattedMessage();
                markup.AddMarkupPermissive(Loc.GetString(ent.Comp.Content));
                _examine.SendExamineTooltip(user, ent, markup, false, false);
            },
            Text = Loc.GetString("lore-examinable-verb-text"),
            Category = VerbCategory.Examine,
            Disabled = !detailsRange,
            Message = detailsRange ? null : Loc.GetString("lore-examinable-verb-disabled"),
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/examine.svg.192dpi.png"))
        };

        args.Verbs.Add(verb);
    }
}
