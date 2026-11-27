using Content.Server.GameTicking;
using Content.Server.Ghost.Roles;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Xenonids.Banish;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Construction.EggMorpher;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Xenonids.Projectile.Parasite;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Robust.Server.Containers;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Xenonids.Parasite;

public sealed partial class XenoEggRoleSystem : EntitySystem
{
    private readonly ProtoId<JobPrototype> _parasiteJob = "CMXenoParasite";
    private TimeSpan _parasiteSpawnDelay;
    private int _parasiteSpawnMinimum;
    private int _parasiteSpawnScale;

    [Dependency] private ActorSystem _actor = default!;
    [Dependency] private SharedXenoBanishSystem _banish = default!;
    [Dependency] private XenoEggSystem _eggSystem = default!;
    [Dependency] private XenoParasiteThrowerSystem _throwerSystem = default!;
    [Dependency] private XenoEvolutionSystem _evolutionSystem = default!;
    [Dependency] private EggMorpherSystem _eggMorpherSystem = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private GhostRoleSystem _ghostRole = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private ContainerSystem _container = default!;

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

        Subs.BuiEvents<XenoParasiteComponent>(XenoParasiteGhostUI.Key, subs =>
        {
            subs.Event<XenoParasiteGhostBuiMsg>(OnParasiteGhostBuiChosen);
        });

        Subs.CVar(_config, RMCCVars.RMCParasiteSpawnInitialDelayMinutes, v => _parasiteSpawnDelay = TimeSpan.FromMinutes(v), true);
        Subs.CVar(_config, RMCCVars.RMCParasiteSpawnMinimum, v => _parasiteSpawnMinimum = v, true);
        Subs.CVar(_config, RMCCVars.RMCParasiteSpawnScale, v => _parasiteSpawnScale = v, true);
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

    private void OnParasiteGhostBuiChosen(Entity<XenoParasiteComponent> ent, ref XenoParasiteGhostBuiMsg args)
    {
        var user = args.Actor;

        if (!SharedChecks(ent, user))
            return;

        if (!(HasComp<ParasiteAIComponent>(ent) || HasComp<ParasiteAIDelayAddComponent>(ent)) ||
            _container.IsEntityInContainer(ent))
            return;

        if (_actor.TryGetSession(user, out var session) && session != null)
        {
            //Uses seperate system due to parasites not having an inherent ghost role
            var newMind = _mind.CreateMind(session.UserId, Name(ent));
            _mind.TransferTo(newMind, ent);
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

        // Limit the amount of player controlled parasites.
        var livingParasites = _evolutionSystem.GetLiving<XenoComponent>(x => x.Comp.Role == _parasiteJob && TryComp<ActorComponent>(x, out var actor) && actor.PlayerSession != null);
        var livingXenos = _evolutionSystem.GetLiving<XenoComponent>(x => x.Comp.CountedInSlots);
        var max = Math.Max(_parasiteSpawnMinimum, livingXenos / _parasiteSpawnScale);
        if (livingParasites >= max)
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-egg-ghost-too-many"), user, user, PopupType.MediumCaution);
            return false;
        }

        // If the player previously successfully infected someone, they bypass the timer check entirely
        if (HasComp<InfectionSuccessComponent>(user))
            return true;

        var timeSinceDeath = _gameTiming.RealTime.Subtract(ghostComp.TimeOfDeath);

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

        if (_actor.TryGetSession(user, out var session) &&
            session != null &&
            !_banish.CanJoinHivePopup(session, ent))
        {
            return false;
        }

        return UserCheck(user);
    }
}
