﻿using Content.Shared._RMC14.Xenonids;
using Content.Shared.Popups;

namespace Content.Shared._RMC14.Light;

public sealed class CMPoweredLightSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LightBurnHandAttemptEvent>(OnLightBurnHandAttempt);
    }

    private void OnLightBurnHandAttempt(ref LightBurnHandAttemptEvent ev)
    {
        ev.Cancelled = true;
        if (!HasComp<XenoComponent>(ev.User))
            _popup.PopupClient(Loc.GetString("cm-light-failed"), ev.Light, ev.User);
    }
}
