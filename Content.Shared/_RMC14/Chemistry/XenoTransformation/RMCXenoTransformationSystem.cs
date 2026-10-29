using Content.Shared._RMC14.Medical.Scanner;
using Content.Shared._RMC14.Synth;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.XenoTransformation;

public sealed class RMCXenoTransformationSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCXenoTransformationComponent, HealthScanGetDiseasesEvent>(OnGetDiseases);
    }

    private void OnGetDiseases(Entity<RMCXenoTransformationComponent> ent, ref HealthScanGetDiseasesEvent args)
    {
        args.Diseases.Add(new HealthScanDisease(
            "rmc-xeno-transformation-scan-name",
            "rmc-xeno-transformation-scan-type",
            "rmc-xeno-transformation-scan-cure",
            ent.Comp.Stage,
            ent.Comp.MaxStage));
    }

    public bool CanContract(EntityUid uid)
    {
        return HasComp<HumanoidAppearanceComponent>(uid) &&
               !HasComp<SynthComponent>(uid) &&
               !HasComp<XenoComponent>(uid) &&
               !HasComp<RMCXenoTransformationResistantComponent>(uid) &&
               !HasComp<RMCXenoTransformationComponent>(uid) &&
               !_mobState.IsDead(uid);
    }

    public bool TryContract(EntityUid uid)
    {
        if (!CanContract(uid))
            return false;

        var comp = EnsureComp<RMCXenoTransformationComponent>(uid);
        comp.Carrier = _random.Prob(comp.CarrierChance);
        comp.NextTick = _timing.CurTime + comp.TickEvery;
        return true;
    }

    public void Cure(Entity<RMCXenoTransformationComponent?> ent, bool resistance = true)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        RemCompDeferred<RMCXenoTransformationComponent>(ent);
        if (resistance)
            EnsureComp<RMCXenoTransformationResistantComponent>(ent);
    }

    public void ReduceStage(Entity<RMCXenoTransformationComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Stage--;
        if (ent.Comp.Stage < 1)
            Cure(ent);
    }
}
