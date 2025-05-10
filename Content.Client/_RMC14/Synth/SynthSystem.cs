using Content.Client.Damage;
using Content.Shared._RMC14.Synth;
using Content.Shared.Damage.Prototypes;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Synth;

public sealed class SynthSystem : SharedSynthSystem
{ // TODO rework this code why is damage visuals client only
    [Dependency] private readonly DamageVisualsSystem _damageVisuals = default!;

    private static readonly ProtoId<DamageGroupPrototype> GroupToChange = "Brute";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SynthComponent, ComponentStartup>(OnCompStartup);
    }

    protected override void MakeSynth(Entity<SynthComponent> ent)
    {
        base.MakeSynth(ent);
    }

    private void OnCompStartup(Entity<SynthComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        if (!TryComp<DamageVisualsComponent>(ent.Owner, out var damageVisuals))
            return;

        _damageVisuals.ChangeDamageGroupColor(sprite, damageVisuals, GroupToChange, ent.Comp.DamageVisualsColor);
    }
}
