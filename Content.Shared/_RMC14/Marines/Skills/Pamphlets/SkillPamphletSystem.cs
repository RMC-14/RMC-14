﻿using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Roles.Jobs;
using Content.Shared.Whitelist;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Marines.Skills.Pamphlets;

public sealed class SkillPamphletSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SquadSystem _squads = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SkillPamphletComponent, UseInHandEvent>(OnUse);

        SubscribeLocalEvent<UsedSkillPamphletComponent, GetMarineIconEvent>(OnGetMarineIcon, after: [typeof(SharedMarineSystem), typeof(SquadSystem)]);
        SubscribeLocalEvent<UsedSkillPamphletComponent, GetMarineSquadNameEvent>(OnGetSquadTitle, after: [typeof(SquadSystem)]);
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

        // Check if the user has a whitelisted job
        var failed = true;
        LocId? popup = null;
        foreach (var whitelist in ent.Comp.JobWhitelists)
        {
            if (_mind.TryGetMind(args.User, out var mindId, out _) && _job.MindHasJobWithId(mindId, whitelist.JobProto))
            {
                failed = false;
            }
            else
            {
                popup = whitelist.Popup;
            }
        }

        if (failed)
        {
            if (popup != null)
                _popup.PopupClient(Loc.GetString(popup), ent, args.User);

            return;
        }

        // Add any components that should be added
        foreach (var comp in ent.Comp.AddComps.Values)
        {
            if (HasComp(args.User, comp.Component.GetType()))
                continue;

            EntityManager.AddComponent(args.User, comp);
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

            var usedSkillComp = EnsureComp<UsedSkillPamphletComponent>(args.User);
            usedSkillComp.Icon = ent.Comp.GiveIcon;
            usedSkillComp.JobTitle = ent.Comp.GiveJobTitle;
            Dirty(args.User, usedSkillComp);

            var mapBlip = EnsureComp<MapBlipIconOverrideComponent>(args.User);
            mapBlip.Icon = ent.Comp.GiveMapBlip;
            Dirty(args.User, mapBlip);

            _squads.UpdateSquadTitle(args.User);

            if (!_net.IsClient)
                QueueDel(ent);

            return;
        }

        _popup.PopupClient(Loc.GetString("rmc-pamphlets-already-know"), ent, args.User);
    }

    private void OnGetMarineIcon(Entity<UsedSkillPamphletComponent> ent, ref GetMarineIconEvent args)
    {
        if (HasComp<SquadLeaderComponent>(ent))
            return;

        if (ent.Comp.Icon == null)
            return;

        args.Icon = ent.Comp.Icon;
    }

    private void OnGetSquadTitle(Entity<UsedSkillPamphletComponent> ent, ref GetMarineSquadNameEvent args)
    {
        if (ent.Comp.JobTitle == null)
            return;

        args.RoleName = Loc.GetString(ent.Comp.JobTitle);
    }
}
