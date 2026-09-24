using Content.Shared._RMC14.Xenonids.Energy;
using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared._RMC14.TrainingDummy;
using Content.Shared.Mobs;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.AciderGeneration;

public sealed class XenoAciderGenerationSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly XenoEnergySystem _energy = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoAciderGenerationComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<XenoAciderGenerationComponent> xeno, ref MeleeHitEvent args)
    {
        var startGenerating = false;
        foreach (var hit in args.HitEntities)
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, hit))
                continue;

            if (HasComp<RMCTrainingDummyComponent>(hit))
                return;

            startGenerating = true;
            break;
        }

        if (!startGenerating)
            return;

        _appearance.SetData(xeno, XenoAcidGeneratingVisuals.Generating, true);
        xeno.Comp.ExpireAt = _timing.CurTime + xeno.Comp.ExpireDuration;
        Dirty(xeno);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;

        var query = EntityQueryEnumerator<XenoAciderGenerationComponent, XenoEnergyComponent>();

        while (query.MoveNext(out var uid, out var acid, out var energy))
        {
            if (acid.ExpireAt == null)
                continue;

            if (time >= acid.NextIncrease)
            {
                _energy.AddEnergy((uid, energy), acid.IncreaseAmount, false);
                acid.NextIncrease = time + acid.TimeBetweenGeneration;
                Dirty(uid, acid);
            }

            if (time < acid.ExpireAt)
                continue;

            acid.ExpireAt = null;
            _appearance.SetData(uid, XenoAcidGeneratingVisuals.Generating, false);
            Dirty(uid, acid);
        }
    }
}
