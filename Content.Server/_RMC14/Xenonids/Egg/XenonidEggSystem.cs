using Content.Shared._RMC14.Hands;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Actions;
using Content.Shared.Buckle.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Ghost;
using Content.Shared.Hands.Components;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Maps;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.StepTrigger.Components;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Content.Server.Ghost.Roles;
using Content.Shared._RMC14.Admin;

namespace Content.Server._RMC14.Xenonids.Egg;

public sealed class XenonidEggSystem : EntitySystem
{
    [Dependency] private readonly ActorSystem _actor = default!;
    [Dependency] private readonly XenoEggSystem _eggSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly GhostRoleSystem _ghostRole = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        Subs.BuiEvents<XenoEggComponent>(XenoEggGhostUI.Key, subs =>
        {
            subs.Event<XenoEggGhostBuiMsg>(OnXenoEggGhostBuiChosen);
        });
    }

    private void OnXenoEggGhostBuiChosen(Entity<XenoEggComponent> ent, ref XenoEggGhostBuiMsg args)
    {
        _ui.CloseUi(ent.Owner, XenoEggGhostUI.Key);

        if (_net.IsClient)
            return;

        var user = args.Actor;

        if (!TryComp(user, out GhostComponent? ghostComp))
            return;

        // Must have been dead for 3 minutes
        if (_gameTiming.RealTime.Subtract(ghostComp.TimeOfDeath) < TimeSpan.FromMinutes(3))
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-egg-ghost-need-time"), user, user, PopupType.MediumCaution);
            return;
        }

        if (_eggSystem.Open(ent, null, out var spawned))
        {
            Dirty(ent);

            if (spawned == null)
                return;

            if (_actor.TryGetSession(user, out var session) && session != null)
                _ghostRole.GhostRoleInternalCreateMindAndTransfer(session, spawned.Value, spawned.Value);
        }
    }
}