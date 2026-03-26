using System.Numerics;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Rangefinder.Spotting;
using Content.Shared._RMC14.Targeting.Focused;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Weapons.Ranged.Sniper.Focused;

public sealed class FocusedOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;
    private readonly IEntityManager _entManager;
    private readonly IPlayerManager _playerManager;
    private readonly SpriteSystem _sprite;
    private readonly IGameTiming _timing;
    private readonly SharedTransformSystem _transform;

    private float _animTime;

    public FocusedOverlay(IEntityManager entManager, IPlayerManager playerManager, IGameTiming timing)
    {
        _entManager = entManager;
        _playerManager = playerManager;
        _entManager = entManager;
        _sprite = entManager.System<SpriteSystem>();
        _timing = timing;
        _transform = entManager.System<SharedTransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var query = _entManager.EntityQueryEnumerator<RMCBeingFocusedComponent>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var squadmemberQuery = _entManager.GetEntityQuery<SquadMemberComponent>();
        var spotterQuery = _entManager.GetEntityQuery<SpotterWhitelistComponent>();
        var worldHandle = args.WorldHandle;

        _animTime += (float)_timing.FrameTime.TotalSeconds;

        while (query.MoveNext(out var uid, out var focused))
        {
            EntityUid? focusingEnt = null;
            foreach (var focusing in focused.FocusedBy)
            {
                if (_playerManager.LocalEntity is not { } player || player != focusing && !spotterQuery.HasComp(player))
                    continue;

                focusingEnt = focusing;
                break;
            }

            if (focusingEnt == null)
                continue;

            if (!xformQuery.TryGetComponent(uid, out var focusedXform))
                continue;

            var focusedRsi = _sprite.GetState(new SpriteSpecifier.Rsi(focused.RsiPath, focused.FocusedState));
            var time = _animTime % focusedRsi.AnimationLength;
            var delay = 0f;
            var frameIndex = 0;
            for (var i = 0; i < focusedRsi.DelayCount; i++)
            {
                delay += focusedRsi.GetDelay(i);
                if (!(time < delay))
                    continue;

                frameIndex = i;
                break;
            }

            var worldPosCross = _transform.GetWorldPosition(focusedXform, xformQuery);

            var focusedTexture = focusedRsi.GetFrames(RsiDirection.South)[frameIndex];
            var centerOffset = new Vector2(focusedTexture.Width / 2f / EyeManager.PixelsPerMeter, focusedTexture.Height / 2f / EyeManager.PixelsPerMeter);

            Color? color = null;
            if (squadmemberQuery.TryGetComponent(focusingEnt, out var squadMember))
            {
                color = squadMember.BackgroundColor;
            }

            worldHandle.DrawTexture(focusedTexture, worldPosCross - centerOffset, color);
        }
    }
}
