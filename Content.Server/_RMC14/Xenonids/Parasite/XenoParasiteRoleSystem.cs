using Content.Server.GameTicking;
using Content.Server.Ghost.Roles;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Xenonids.Construction.EggMorpher;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Xenonids.Projectile.Parasite;
using Content.Shared.Ghost;
using Content.Shared.Popups;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Xenonids.Parasite;

public sealed class XenoEggRoleSystem : EntitySystem
{
    private TimeSpan _parasiteSpawnDelay;

    [Dependency] private readonly ActorSystem _actor = default!;
    [Dependency] private readonly XenoEggSystem _eggSystem = default!;
    [Dependency] private readonly XenoParasiteThrowerSystem _throwerSystem = default!;
    [Dependency] private readonly EggMorpherSystem _eggMorpherSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly GhostRoleSystem _ghostRole = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;

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

        Subs.BuiEvents<EggMorpherComponent>(XenoParasiteGhostUI.Key, subs =>
        {
            subs.Event<XenoParasiteGhostBuiMsg>(OnEggMorpherGhostBuiChosen);
        });

        Subs.CVar(_config, RMCCVars.RMCParasiteSpawnInitialDelayMinutes, v => _parasiteSpawnDelay = TimeSpan.FromMinutes(v), true);
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

    private void OnEggMorpherGhostBuiChosen(Entity<EggMorpherComponent> ent, ref XenoParasiteGhostBuiMsg args)
    {
        var user = args.Actor;

        if (!SharedChecks(ent, user))
            return;

        if (ent.Comp.CurParasites > ent.Comp.ReservedParasites &&
            _eggMorpherSystem.TryCreateParasiteFromEggMorpher(ent, out var parasite) &&
            parasite != null &&
            _actor.TryGetSession(user, out var session) &&
            session != null)
        {
            _ghostRole.GhostRoleInternalCreateMindAndTransfer(session, parasite.Value, parasite.Value);
        }
    }

    /// <summary>
    /// Can this user take a parasite role
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public bool UserCheck(EntityUid user)
    {
        if (_net.IsClient)
            return false;

        if (!TryComp(user, out GhostComponent? ghostComp))
            return false;

        // Checks if the round has been going on long enough to allow player controlled parasites.
        if (_gameTicker.RoundDuration() <= _parasiteSpawnDelay)
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-egg-ghost-need-time-round", ("seconds", (int)(_parasiteSpawnDelay.TotalSeconds - _gameTicker.RoundDuration().TotalSeconds))), user, user, PopupType.MediumCaution);
            return false;
        }

        // If the player previously successfully infected someone, they bypass the timer check entirely
        if (HasComp<InfectionSuccessComponent>(user))
            return true;

        var timeSinceDeath = _gameTiming.CurTime.Subtract(ghostComp.TimeOfDeath);

        // Must have been dead for 3 minutes
        if (timeSinceDeath < TimeSpan.FromMinutes(3))
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-egg-ghost-need-time", ("seconds", 180 - (int)timeSinceDeath.TotalSeconds)), user, user, PopupType.MediumCaution);
            return false;
        }

        return true;
    }
    private bool SharedChecks(EntityUid ent, EntityUid user)
    {
        //TODO RMC14 parasite bans should be checked here
        _ui.CloseUi(ent, XenoParasiteGhostUI.Key);

        return UserCheck(user);
    }
}
