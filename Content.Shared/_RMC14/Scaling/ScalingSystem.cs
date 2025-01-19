using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Vendors;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
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

    private float _marineScalingNormal;
    private float _marineScalingBonus;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);

        Subs.CVar(_config, RMCCVars.RMCMarineScalingNormal, v => _marineScalingNormal = v, true);
        Subs.CVar(_config, RMCCVars.RMCMarineScalingBonus, v => _marineScalingBonus = v, true);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (!ev.LateJoin || !HasComp<MarineComponent>(ev.Mob))
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
            scaling.Scale += job.RoleWeight * (0.25 + 0.75 / (1 + deciseconds / 20000)) / _marineScalingNormal;
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
            if (userIFF.Faction != faction)
                continue;

            if (!_mind.TryGetMind(marineId, out var mindId, out _) ||
                !_job.MindTryGetJob(mindId, out var job))
            {
                continue;
            }

            marineCount += job.RoleWeight;
        }

        scaling.Comp.Scale = Math.Max(1, marineCount / _marineScalingNormal);
        scaling.Comp.MaxScale = scaling.Comp.Scale;

        var vendors = EntityQueryEnumerator<CMAutomatedVendorComponent>();
        while (vendors.MoveNext(out var vendorId, out var vendor))
        {
            foreach (var section in vendor.Sections)
            {
                for (var i = 0; i < section.Entries.Count; i++)
                {
                    var entry = section.Entries[i];
                    if (entry.Amount is not { } amount)
                        continue;

                    amount = (int) Math.Round(amount * scaling.Comp.Scale);
                    section.Entries[i] = entry with
                    {
                        Amount = amount,
                        Max = amount,
                    };
                }
            }

            Dirty(vendorId, vendor);
        }
    }
}
