using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;

namespace Content.Shared._RMC14.Repairable;

public sealed class RMCWeldableSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WeldableComponent, WeldableAttemptEvent>(OnWeldableAttempt);
    }

    private void OnWeldableAttempt(Entity<WeldableComponent> ent, ref WeldableAttemptEvent args)
    {
        if (!TryComp<RMCWeldFuelComponent>(args.Tool, out var welderFuel))
            return;

        var baseFuel = MetaData(ent).EntityPrototype is { } proto &&
                       proto.TryGetComponent<WeldableComponent>(out var protoWeldable, _compFactory)
            ? protoWeldable.Fuel
            : ent.Comp.Fuel;

        ent.Comp.Fuel = Math.Max(baseFuel * welderFuel.WeldFuelMultiplier, welderFuel.MinWeldFuel);
    }
}
