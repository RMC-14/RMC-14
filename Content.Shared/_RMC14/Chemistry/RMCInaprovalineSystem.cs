using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Damage;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry;

public sealed class RMCInaprovalineSystem : EntitySystem
{
    [Dependency] private readonly SharedRMCBloodstreamSystem _bloodstream = default!;

    private static readonly ProtoId<ReagentPrototype> Inaprovaline = "CMInaprovaline";
    private static readonly ProtoId<DamageTypePrototype> AsphyxiationType = "Asphyxiation";

    public override void Initialize()
    {
        SubscribeLocalEvent<BloodstreamComponent, DamageStateCritBeforeDamageEvent>(OnCritBeforeDamage);
    }

    private void OnCritBeforeDamage(Entity<BloodstreamComponent> ent, ref DamageStateCritBeforeDamageEvent args)
    {
        if (!args.Damage.DamageDict.ContainsKey(AsphyxiationType) ||
            !_bloodstream.TryGetChemicalSolution(ent, out _, out var solution) ||
            solution.GetTotalPrototypeQuantity(Inaprovaline) <= FixedPoint2.Zero)
        {
            return;
        }

        var damage = new DamageSpecifier(args.Damage);
        damage.DamageDict.Remove(AsphyxiationType);
        args.Damage = damage;
    }
}
