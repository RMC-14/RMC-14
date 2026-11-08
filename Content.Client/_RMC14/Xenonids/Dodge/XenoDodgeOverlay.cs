using Content.Shared._RMC14.Xenonids.Dodge;
using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared.Mobs.Systems;
using Content.Shared.Standing;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Client._RMC14.Xenonids.Dodge;

public sealed class XenoDodgeOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    private readonly IEntityManager _entManager;
    private readonly SpriteSystem _sprite;
    private readonly SharedTransformSystem _transform;
    private readonly IGameTiming _timing;
    private readonly IRobustRandom _random;
    private readonly MobStateSystem _mob;
    private readonly StandingStateSystem _stand;


    public XenoDodgeOverlay(IEntityManager entManager, IGameTiming timing, IRobustRandom random)
    {
        IoCManager.InjectDependencies(this);

        _entManager = entManager;
        _timing = timing;
        _random = random;

        _sprite = entManager.System<SpriteSystem>();
        _transform = entManager.System<SharedTransformSystem>();
        _mob = entManager.System<MobStateSystem>();
        _stand = entManager.System<StandingStateSystem>();
    }

    //Draws and Handles active dodge afterimages
    protected override void Draw(in OverlayDrawArgs args)
    {
        var query = _entManager.EntityQueryEnumerator<XenoActiveDodgeComponent, SpriteComponent>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var worldHandle = args.WorldHandle;

        var eye = args.Viewport.Eye;
        var eyeRot = eye?.Rotation ?? default;

        var time = _timing.CurTime;

        while (query.MoveNext(out var uid, out var dodge, out var sprite))
        {
            if (time < dodge.NextOffsetChange)
            {
                RenderAfterImage((uid, dodge), sprite, time, args.WorldHandle, eyeRot);
                continue;
            }

            if (_entManager.HasComponent<XenoRestingComponent>(uid) || !_mob.IsAlive(uid) || _stand.IsDown(uid))
                _sprite.SetOffset((uid, sprite), Vector2.Zero);
            else
            {
                _sprite.SetOffset((uid, sprite), new Vector2(_random.Next(-4, 5), _random.Next(-4, 5)) * 0.01f);
            }

            if (!xformQuery.TryGetComponent(uid, out var xform))
                continue;

            if (dodge.LastPosition != null)
            {
                dodge.AfterImages.Add(new RMCAfterImage
                   (dodge.LastPosition.Value.WorldPosition,
                   dodge.LastPosition.Value.WorldAngle,
                   time + dodge.AfterImageDuration,
                   sprite.Offset
                   ));
            }

            dodge.LastPosition = _transform.GetWorldPositionRotation(uid);
            dodge.NextOffsetChange = time + dodge.TimeBetweenOffsets;

            RenderAfterImage((uid, dodge), sprite, time, args.WorldHandle, eyeRot);
        }
    }

    private void RenderAfterImage(Entity<XenoActiveDodgeComponent> xeno, SpriteComponent sprite, TimeSpan currTime, DrawingHandleWorld handle, Angle eye)
    {
        var offset = sprite.Offset;
        var colorCache = sprite.Color;

        List<RMCAfterImage> remove = new();

        foreach (var afterimage in xeno.Comp.AfterImages)
        {
            if (currTime >= afterimage.DisappearTime)
            {
                remove.Add(afterimage);
                continue;
            }

            var color = sprite.Color * Color.White.WithAlpha((float)(((afterimage.DisappearTime - currTime) / xeno.Comp.AfterImageDuration) * xeno.Comp.AfterImageOpacityMult));
            _sprite.SetColor((xeno, sprite), color);

            _sprite.SetOffset((xeno, sprite), afterimage.Offset);
            _sprite.RenderSprite((xeno, sprite), handle, eye, afterimage.WorldAngle, afterimage.WorldPosition);

            _sprite.SetColor((xeno, sprite), colorCache);
        }

        foreach (var removal in remove)
            xeno.Comp.AfterImages.Remove(removal);

        _sprite.SetOffset((xeno, sprite), offset);
    }

}
