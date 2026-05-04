using Robust.Client.GameObjects;

namespace Content.Client._RMC14;

public sealed class RotationDrawDepthSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<RotationDrawDepthComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var rotation, out _, out var xform))
        {
            // TODO RMC14 this needs to support rotated viewports eventually
            var dir = xform.LocalRotation.GetCardinalDir();
            switch (dir)
            {
                case Direction.South:
                    _sprite.SetDrawDepth(uid, rotation.SouthDrawDepth);
                    break;
                default:
                    _sprite.SetDrawDepth(uid, rotation.DefaultDrawDepth);
                    break;
            }
        }
    }
}
