using Content.Client.Damage;
using Content.Shared._RMC14.Synth;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Synth;

public sealed class SynthSystem : SharedSynthSystem
{ // TODO rework this code why is damage visuals client only
    [Dependency] private readonly DamageVisualsSystem _damageVisuals = default!;

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

        _damageVisuals.ChangeDamageGroupColor(sprite, damageVisuals, "Brute", ent.Comp.DamageVisualsColor);
    }
}
