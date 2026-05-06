using Content.Server._RMC14.GameStates;
using Content.Shared._RMC14.Tether;

namespace Content.Server._RMC14.Tether;

public sealed class RMCTetherSystem : SharedRMCTetherSystem
{
    [Dependency] private readonly RMCPvsSystem _pvs = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCTetherComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCTetherComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnMapInit(Entity<RMCTetherComponent> ent, ref MapInitEvent ev)
    {
        _pvs.AddGlobalOverride(ent);
    }

    private void OnComponentRemove(Entity<RMCTetherComponent> ent, ref ComponentRemove ev)
    {
        _pvs.RemoveGlobalOverride(ent);
    }
}
