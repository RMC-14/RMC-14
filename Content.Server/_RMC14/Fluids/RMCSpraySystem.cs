using Content.Server.Fluids.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Shared._RMC14.Fluids;
using Robust.Shared.Map;

namespace Content.Server._RMC14.Fluids;

public sealed class RMCSpraySystem : SharedRMCSpraySystem
{
    [Dependency] private readonly SpraySystem _spray = default!;

    public override void Spray(EntityUid entity, EntityUid user, MapCoordinates mapcoord, bool hitUser = false)
    {
        base.Spray(entity, user, mapcoord);

        if (TryComp(entity, out SprayComponent? spray))
            _spray.Spray((entity, spray), user, mapcoord, hitUser);
    }
}
