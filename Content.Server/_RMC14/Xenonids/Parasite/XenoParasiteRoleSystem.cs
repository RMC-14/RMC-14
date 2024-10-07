using Content.Server.Ghost.Roles;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Projectile.Parasite;
using Content.Shared.Ghost;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Xenonids.Parasite;

public sealed class XenoEggRoleSystem : EntitySystem
{
    [Dependency] private readonly ActorSystem _actor = default!;
    [Dependency] private readonly XenoEggSystem _eggSystem = default!;
    [Dependency] private readonly XenoParasiteThrowerSystem _throwerSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly GhostRoleSystem _ghostRole = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        Subs.BuiEvents<XenoEggComponent>(XenoParasiteGhostUI.Key, subs =>
        {
            subs.Event<XenoParasiteGhostBuiMsg>(OnXenoEggGhostBuiChosen);
        });

        Subs.BuiEvents<XenoParasiteThrowerComponent>(XenoParasiteGhostUI.Key, subs =>
        {
            subs.Event<XenoParasiteGhostBuiMsg>(OnXenoCarrierGhostBuiChosen);
        });
    }

    private void OnXenoEggGhostBuiChosen(Entity<XenoEggComponent> ent, ref XenoParasiteGhostBuiMsg args)
    {
        var user = args.Actor;

        if (!SharedChecks(ent, user))
            return;

        if (_eggSystem.Open(ent, null, out var spawned))
        {
            Dirty(ent);

            if (spawned == null)
                return;

            if (_actor.TryGetSession(user, out var session) && session != null)
                _ghostRole.GhostRoleInternalCreateMindAndTransfer(session, spawned.Value, spawned.Value);
        }
    }

    private void OnXenoCarrierGhostBuiChosen(Entity<XenoParasiteThrowerComponent> ent, ref XenoParasiteGhostBuiMsg args)
    {
        var user = args.Actor;

        if (!SharedChecks(ent, user))
            return;

        if (_throwerSystem.TryRemoveGhostParasite(ent, out string msg) is { } parasite)
        {
            if (_actor.TryGetSession(user, out var session) && session != null)
                _ghostRole.GhostRoleInternalCreateMindAndTransfer(session, parasite, parasite);
        }
        else
            _popup.PopupEntity(msg, user, user, PopupType.MediumCaution);
    }

    private bool SharedChecks(EntityUid ent, EntityUid user)
    {
        //TODO RMC14 parasite bans should be checked here
        _ui.CloseUi(ent, XenoParasiteGhostUI.Key);

        if (_net.IsClient)
            return false;

        if (!TryComp(user, out GhostComponent? ghostComp))
            return false;

        // Must have been dead for 3 minutes
        if (_gameTiming.CurTime.Subtract(ghostComp.TimeOfDeath) < TimeSpan.FromMinutes(3))
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-egg-ghost-need-time"), user, user, PopupType.MediumCaution);
            return false;
        }

        return true;
    }
}