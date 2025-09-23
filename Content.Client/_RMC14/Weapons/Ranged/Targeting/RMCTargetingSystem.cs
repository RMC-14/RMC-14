using System.Linq;
using Content.Shared._RMC14.Targeting;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client._RMC14.Weapons.Ranged.Targeting;

public sealed class RMCTargetingSystem : SharedRMCTargetingSystem
{
    private const string TargetedKey = "targetedDirection";
    private const string TargetedDirectionKey = "targetedDirectionIntense";

    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new TargetingOverlay(EntityManager));
        SubscribeLocalEvent<RMCTargetedComponent, GotTargetedEvent>(OnGotTargeted);
    }

    /// <summary>
    ///     Rotate the visualizer to the cardinal direction closest to the targeting entity.
    /// </summary>
    private void OnGotTargeted(Entity<RMCTargetedComponent> ent, ref GotTargetedEvent args)
    {
        if(!TryComp(ent, out SpriteComponent? sprite))
            return;

        if(!sprite.LayerExists(TargetedKey) || !sprite.LayerExists(TargetedDirectionKey))
            return;

        var coords = _transform.GetMoverCoordinateRotation(ent, Transform(ent));
        var sourceCoords = _transform.GetMoverCoordinates(ent.Comp.TargetedBy.Last());
        var direction = coords.Coords.Position - sourceCoords.Position;
        var angle = direction.ToAngle().GetCardinalDir().GetClockwise90Degrees().ToAngle() - coords.worldRot;

        sprite.LayerSetRotation(TargetedKey, angle);
        sprite.LayerSetRotation(TargetedDirectionKey, angle);
    }
}
