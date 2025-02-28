﻿using Content.Shared.Verbs;

namespace Content.Shared._RMC14.Radio;

public sealed class SharedRMCRadioFilterSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCRadioFilterComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);

        Subs.BuiEvents<RMCRadioFilterComponent>(RMCRadioFilterUI.Key,
            subs =>
            {
                subs.Event<RMCRadioFilterBuiMsg>(OnRadioFilterBuiMsg);
            });
    }

    private void OnGetAltVerbs(Entity<RMCRadioFilterComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb()
        {
            Text = "Tune Radio",
            IconEntity = GetNetEntity(ent.Owner),
            Act = () =>
            {
                _ui.OpenUi(ent.Owner, RMCRadioFilterUI.Key, user);
            },
        });
    }

    private void OnRadioFilterBuiMsg(Entity<RMCRadioFilterComponent> ent, ref RMCRadioFilterBuiMsg args)
    {
        if (args.Toggle)
        {
            ent.Comp.DisabledChannels.Remove(args.Channel);
        }
        else
        {
            ent.Comp.DisabledChannels.Add(args.Channel);
        }

        Dirty(ent);
    }
}
