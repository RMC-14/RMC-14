using Robust.Shared.Map;

namespace Content.Shared._RMC14.Fluids;

public abstract class SharedRMCSpraySystem : EntitySystem
{
    public virtual void Spray(EntityUid entity, EntityUid user, MapCoordinates mapcoord, bool hitUser = false)
    {
    }
}
