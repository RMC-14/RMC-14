using Content.Shared.Interaction;
using Content.Shared.Lock;

namespace Content.Shared._RMC14.SecureSafe;

public abstract class SharedRMCSafeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCSafeComponent, ActivateInWorldEvent>(OnActivateInWorld, before: [typeof(LockSystem)]);
    }

    private void OnActivateInWorld(Entity<RMCSafeComponent> ent, ref ActivateInWorldEvent args)
    {
        // Handled server-side via ActivatableUI
    }
}
