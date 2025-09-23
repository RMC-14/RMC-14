using Content.Server.Atmos.EntitySystems;
using Content.Shared._RMC14.Atmos;
using Content.Shared.Atmos.Components;

namespace Content.Server._RMC14.Atmos;

public sealed class RMCFlammableSystem : SharedRMCFlammableSystem
{
    [Dependency] private readonly FlammableSystem _flammable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlammableComponent, ShowFireAlertEvent>(OnShowFireAlert);
    }

    private void OnShowFireAlert(Entity<FlammableComponent> ent, ref ShowFireAlertEvent args)
    {
        if (ent.Comp.OnFire)
            args.Show = true;
    }

    public override bool Ignite(Entity<FlammableComponent?> flammable, int intensity, int duration, int? maxStacks, bool igniteDamage = true)
    {
        base.Ignite(flammable, intensity, duration, maxStacks);

        if (!Resolve(flammable, ref flammable.Comp, false))
            return false;

        var hadBypassComponent = HasComp<RMCFireBypassActiveComponent>(flammable);

        var stacks = flammable.Comp.FireStacks + duration;
        if (maxStacks != null && stacks > maxStacks)
            stacks = maxStacks.Value;

        _flammable.SetFireStacks(flammable, stacks, flammable, true);
        if (!flammable.Comp.OnFire)
            return false;

        if (hadBypassComponent)
        {
            EnsureComp<RMCFireBypassActiveComponent>(flammable);
        }

        flammable.Comp.Intensity = intensity;
        flammable.Comp.Duration = duration;
        return true;
    }

    public override void Extinguish(Entity<FlammableComponent?> flammable)
    {
        base.Extinguish(flammable);

        if (!Resolve(flammable, ref flammable.Comp, false))
            return;

        _flammable.Extinguish(flammable, flammable);
    }

    public override void Pat(Entity<FlammableComponent?> flammable, int stacks)
    {
        if (!Resolve(flammable, ref flammable.Comp, false))
            return;

        _flammable.AdjustFireStacks(flammable, stacks, flammable);
    }
}
