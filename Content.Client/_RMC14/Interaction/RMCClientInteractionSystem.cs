using Content.Shared._RMC14.Interaction;
using Robust.Client.GameObjects;
using Robust.Shared.Graphics;

namespace Content.Client._RMC14.Interaction;

public sealed partial class RMCClientInteractionSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public bool IsInteractionTransparency(EntityUid target, EntityUid? localEntity, IEye? eye)
    {
        if (localEntity is not { } user ||
            eye == null ||
            !HasComp<InteractionTransparencyComponent>(target))
        {
            return false;
        }

        if (!TryComp(target, out TransformComponent? entXform) ||
            !TryComp(target, out SpriteComponent? sprite) ||
            !TryComp(user, out TransformComponent? playerXform))
        {
            return false;
        }

        var (spritePos, spriteRot) = _transform.GetWorldPositionRotation(entXform);
        var spriteBox = _sprite.CalculateBounds((target, sprite), spritePos, spriteRot, eye.Rotation);
        var playerPos = _transform.GetMapCoordinates(playerXform).Position;

        return spriteBox.Contains(playerPos);
    }
}
