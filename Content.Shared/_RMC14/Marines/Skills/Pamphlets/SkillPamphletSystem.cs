using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Marines.Skills.Pamphlets;

public sealed class SkillPamphletSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SkillPamphletComponent, UseInHandEvent>(OnUse);
    }

    private void OnUse(Entity<SkillPamphletComponent> ent, ref UseInHandEvent args)
    {
        args.Handled = true;

        // Then see if they've reached the limit (or if it applies at all)
        if (!ent.Comp.BypassLimit && HasComp<UsedSkillPamphletComponent>(args.User))
        {
            _popup.PopupClient(Loc.GetString("rmc-pamphlets-limit-reached"), ent, args.User);
            return;
        }

        // Next go through the EntityWhitelist that's attached, if any, and deny them for the attached reason
        foreach (var whitelist in ent.Comp.Whitelists)
        {
            if (_whitelistSystem.IsWhitelistFail(whitelist.Restrictions, args.User))
            {
                _popup.PopupClient(Loc.GetString(whitelist.Popup), ent, args.User);
                return;
            }
        }

        // Add any components that should be added
        foreach (var comp in ent.Comp.AddComps.Values)
        {
            if (HasComp(args.User, comp.GetType()))
                continue;

            AddComp(args.User, comp.Component);
            ent.Comp.GaveSkill = true;
        }

        // Add any unknown skills
        foreach (var skill in ent.Comp.AddSkills)
        {
            if (_skills.HasSkill(args.User, skill.Key, skill.Value))
                continue;

            _skills.SetSkill(args.User, skill.Key, skill.Value);
            ent.Comp.GaveSkill = true;
        }

        if (ent.Comp.GaveSkill)
        {
            _popup.PopupClient(Loc.GetString("rmc-pamphlets-reading"), args.User, args.User);
            EnsureComp<UsedSkillPamphletComponent>(args.User);
            if(!_net.IsClient)
                QueueDel(ent);

            return;
        }

        _popup.PopupClient(Loc.GetString("You know this already!"), ent, args.User);
    }
}
