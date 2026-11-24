using Content.Server._RMC14.GameStates;
using Content.Shared._RMC14.Tether;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Tether;

public sealed class RMCTetherSystem : SharedRMCTetherSystem
{
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly RMCPvsSystem _pvs = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCTetherComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCTetherComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnMapInit(Entity<RMCTetherComponent> ent, ref MapInitEvent ev)
    {
        foreach (var session in _player.Sessions)
        {
            if (session.AttachedEntity == null)
                continue;

            _pvs.AddSessionOverride(ent, session);
        }
    }

    private void OnComponentRemove(Entity<RMCTetherComponent> ent, ref ComponentRemove ev)
    {
        foreach (var session in _player.Sessions)
        {
            _pvs.RemoveSessionOverride(ent, session);
        }
    }
}
