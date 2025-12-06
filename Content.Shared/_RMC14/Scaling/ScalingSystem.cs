using Content.Shared._RMC14.Admin;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Requisitions;
using Content.Shared._RMC14.Requisitions.Components;
using Content.Shared._RMC14.Vendors;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Scaling;

public sealed class ScalingSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedRequisitionsSystem _requisitions = default!;
    [Dependency] private readonly SharedCMAutomatedVendorSystem _rmcAutomatedVendor = default!;
    [Dependency] private readonly GunIFFSystem _gunIFF = default!;

    public float MarineScalingNormal { get; private set; }
    private float _marineScalingBonus;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);

        Subs.CVar(_config, RMCCVars.RMCMarineScalingNormal, v => MarineScalingNormal = v, true);
        Subs.CVar(_config, RMCCVars.RMCMarineScalingBonus, v => _marineScalingBonus = v, true);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (!ev.LateJoin || !HasComp<MarineComponent>(ev.Mob))
            return;

        if (HasComp<RMCAdminSpawnedComponent>(ev.Mob))
            return;

        if (ev.JobId is not { } jobId ||
            !_prototypes.TryIndex(jobId, out JobPrototype? job) ||
            job.RoleWeight <= 0)
        {
            return;
        }

        var scalingQuery = EntityQueryEnumerator<MarineScalingComponent>();
        while (scalingQuery.MoveNext(out var uid, out var scaling))
        {
            var deciseconds = _gameTicker.RoundDuration().TotalSeconds * 10;
            scaling.Scale += job.RoleWeight * (0.25 + 0.75 / (1 + deciseconds / 20000)) / MarineScalingNormal;
            var delta = scaling.Scale - scaling.MaxScale;
            if (delta > 0)
            {
                scaling.MaxScale = scaling.Scale;
                var scaleEv = new MarineScaleChangedEvent(scaling.MaxScale, delta);
                RaiseLocalEvent(ref scaleEv);
            }

            Dirty(uid, scaling);
        }
    }

    public bool TryGetScaling(out Entity<MarineScalingComponent> scaling)
    {
        var query = EntityQueryEnumerator<MarineScalingComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            scaling = (uid, comp);
            return true;
        }

        scaling = default;
        return false;
    }

    private Entity<MarineScalingComponent> EnsureScaling()
    {
        var query = EntityQueryEnumerator<MarineScalingComponent>();
        while (query.MoveNext(out var uid, out var scaling))
        {
            return (uid, scaling);
        }

        var scalingId = Spawn(null, MapCoordinates.Nullspace);
        var scalingComp = EnsureComp<MarineScalingComponent>(scalingId);
        return (scalingId, scalingComp);
    }

    public void TryStartScaling(EntProtoId<IFFFactionComponent> faction)
    {
        var scaling = EnsureScaling();
        if (scaling.Comp.Started)
            return;

        scaling.Comp.Started = true;
        Dirty(scaling);

        var marineCount = _marineScalingBonus;
        var marines = EntityQueryEnumerator<UserIFFComponent, ActorComponent>();
        while (marines.MoveNext(out var marineId, out var userIFF, out _))
        {
            if (!_gunIFF.IsInFaction((marineId, userIFF), faction))
                continue;

            if (!_mind.TryGetMind(marineId, out var mindId, out _) ||
                !_job.MindTryGetJob(mindId, out var job))
            {
                continue;
            }

            marineCount += job.RoleWeight;
        }

        scaling.Comp.Scale = Math.Max(1, marineCount / MarineScalingNormal);
        scaling.Comp.MaxScale = scaling.Comp.Scale;

        var accounts = EntityQueryEnumerator<RequisitionsAccountComponent>();
        while (accounts.MoveNext(out var uid, out var account))
        {
            _requisitions.StartAccount((uid, account), scaling.Comp.Scale, marineCount);
        }

        var vendors = EntityQueryEnumerator<CMAutomatedVendorComponent>();
        while (vendors.MoveNext(out var vendorId, out var vendor))
        {
            var scale = scaling.Comp.Scale;
            if (!vendor.Scaling)
                scale = 1;

            foreach (var section in vendor.Sections)
            {
                for (var i = 0; i < section.Entries.Count; i++)
                {
                    var entry = section.Entries[i];
                    if (entry.Amount is not { } amount || entry.Box != null)
                        continue;

                    amount = (int) Math.Round(amount * scale);
                    section.Entries[i] = entry with
                    {
                        Amount = amount,
                        Max = amount,
                    };

                    _rmcAutomatedVendor.AmountUpdated((vendorId, vendor), entry);
                }
            }

            Dirty(vendorId, vendor);
        }
    }

    public int GetAliveHumanoids()
    {
        var alive = 0;
        var query = EntityQueryEnumerator<MarineComponent, MobStateComponent>();
        while (query.MoveNext(out _, out var mobState))
        {
            if (mobState.CurrentState == MobState.Dead)
                continue;

            alive++;
        }

        return alive;
    }
}
