using Content.Shared._RMC14.Weapons.Ranged.DualTube;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Client._RMC14.Weapons.Ranged.DualTube;

public sealed class RMCDualTubeAmmoCounterSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gun = default!;

    private readonly HashSet<EntityUid> _toRefresh = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCDualTubeComponent, AfterAutoHandleStateEvent>(OnAfterState);
    }

    private void OnAfterState(Entity<RMCDualTubeComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _toRefresh.Add(ent);
    }

    public override void FrameUpdate(float frameTime)
    {
        if (_toRefresh.Count == 0)
            return;

        foreach (var uid in _toRefresh)
        {
            if (!TerminatingOrDeleted(uid))
                _gun.UpdateAmmoCount(uid, prediction: false);
        }

        _toRefresh.Clear();
    }
}
