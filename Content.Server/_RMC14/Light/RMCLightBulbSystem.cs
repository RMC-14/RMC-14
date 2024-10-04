using Content.Server.Light.EntitySystems;
using Content.Shared.Light.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Server.Audio;

namespace Content.Server._RMC14.Light;

public sealed class RMCLightBulbSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly LightBulbSystem _lightBulb = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCBreakLightOnAttackComponent, AttackedEvent>(OnBreakLightAttacked);
    }

    private void OnBreakLightAttacked(Entity<RMCBreakLightOnAttackComponent> ent, ref AttackedEvent args)
    {
        if (!TryComp(ent, out LightBulbComponent? lightBulb) ||
            lightBulb.State == LightBulbState.Broken)
        {
            return;
        }

        _lightBulb.SetState(ent, LightBulbState.Broken, lightBulb);
        _audio.PlayPvs(ent.Comp.Sound, ent);
    }
}
