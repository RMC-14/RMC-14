using Content.Server.Body.Systems;
using Content.Shared._RMC14.Humanoid;
using Content.Shared._RMC14.Synth;
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

        _damageable.SetDamageModifierSetId(ent, ent.Comp.NewDamageModifier);

        // This makes it so the synth doesn't take bloodloss damage.
        _bloodstream.SetBloodLossThreshold(ent, 0f);
        _bloodstream.ChangeBloodReagent(ent, ent.Comp.NewBloodReagent);

        var repOverrideComp = EnsureComp<RMCHumanoidRepresentationOverrideComponent>(ent);
        repOverrideComp.Species = ent.Comp.SpeciesName;
        repOverrideComp.Age = ent.Comp.Generation;
        Dirty(ent, repOverrideComp);

        var organs = _body.GetBodyOrganEntityComps<OrganComponent>(ent.Owner);

        foreach (var organ in organs)
        {
            QueueDel(organ); // Synths do not metabolize chems or breathe
        }

        var headSlots = _body.GetBodyChildrenOfType(ent, BodyPartType.Head);

        foreach (var part in headSlots)
        {
            var newBrain = SpawnNextToOrDrop(ent.Comp.NewBrain, ent);
            _body.AddOrganToFirstValidSlot(part.Id, newBrain);
        }
    }
}
