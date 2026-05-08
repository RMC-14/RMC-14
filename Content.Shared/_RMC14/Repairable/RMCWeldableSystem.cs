using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;

namespace Content.Shared._RMC14.Repairable;

public sealed class RMCWeldableSystem : EntitySystem
{
    private readonly Dictionary<EntityUid, float> _baseFuels = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WeldableComponent, WeldableAttemptEvent>(OnWeldableAttempt);
        SubscribeLocalEvent<WeldableComponent, EntityTerminatingEvent>(OnWeldableTerminating);
    }

    private void OnWeldableAttempt(Entity<WeldableComponent> ent, ref WeldableAttemptEvent args)
    {
        if (!TryComp<RMCBlowtorchWeldFuelComponent>(args.Tool, out var blowtorchFuel))
            return;

        _baseFuels.TryAdd(ent, ent.Comp.Fuel);
        ent.Comp.Fuel = Math.Max(_baseFuels[ent] * blowtorchFuel.WeldFuelMultiplier, blowtorchFuel.MinWeldFuel);
    }

    private void OnWeldableTerminating(Entity<WeldableComponent> ent, ref EntityTerminatingEvent args)
    {
        _baseFuels.Remove(ent);
    }
}
