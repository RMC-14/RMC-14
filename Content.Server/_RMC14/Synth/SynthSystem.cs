using Content.Server.Body.Systems;
using Content.Shared._RMC14.Humanoid;
using Content.Shared._RMC14.Synth;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;

namespace Content.Server._RMC14.Synth;

public sealed class SynthSystem : SharedSynthSystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;

    protected override void MakeSynth(Entity<SynthComponent> ent)
    {
        base.MakeSynth(ent);

        if (TryComp<DamageableComponent>(ent.Owner, out var damageable))
            _damageable.SetDamageModifierSetId(ent.Owner, ent.Comp.NewDamageModifier, damageable);

        if (TryComp<BloodstreamComponent>(ent.Owner, out var bloodstream)) // These TryComps are so tests don't fail
        {
            // This makes it so the synth doesn't take bloodloss damage.
            _bloodstream.SetBloodLossThreshold((ent, bloodstream), 0f);
            _bloodstream.ChangeBloodReagent((ent, bloodstream), ent.Comp.NewBloodReagent);
        }

        var repOverrideComp = EnsureComp<RMCHumanoidRepresentationOverrideComponent>(ent);
        repOverrideComp.Species = ent.Comp.SpeciesName;
        repOverrideComp.Age = ent.Comp.Generation;
        Dirty(ent, repOverrideComp);

        if (!TryComp<BodyComponent>(ent.Owner, out var body))
            return;

        var organs = _body.GetBodyOrganEntityComps<OrganComponent>((ent.Owner, body));

        foreach (var organ in organs)
        {
            QueueDel(organ); // Synths do not metabolize chems or breathe
        }

        var newBrain = SpawnNextToOrDrop(ent.Comp.NewBrain, ent.Owner);

        var headSlots = _body.GetBodyChildrenOfType(ent.Owner, BodyPartType.Head, body);
        foreach (var part in headSlots)
        {
            _body.AddOrganToFirstValidSlot(part.Id, newBrain, part.Component);
            break;
        }
    }
}
