using System.Numerics;
using Content.Shared._RMC14.CrashLand;
using Content.Shared._RMC14.Mobs;
using Content.Shared._RMC14.Sprite;
using Content.Shared._RMC14.Xenonids.Hide;
using Content.Shared.Ghost;
using Content.Shared.ParaDrop;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Map;

namespace Content.Client._RMC14.Sprite;

public sealed class RMCSpriteSystem : SharedRMCSpriteSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCMobStateDrawDepthComponent, AppearanceChangeEvent>(OnDrawDepthAppearanceChange);
    }

    private void OnDrawDepthAppearanceChange(Entity<RMCMobStateDrawDepthComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!args.AppearanceData.ContainsKey(RMCSpriteDrawDepth.Key))
            return;

        UpdateDrawDepth(ent);
    }

    public override Shared.DrawDepth.DrawDepth UpdateDrawDepth(EntityUid sprite)
    {
        var depth = base.UpdateDrawDepth(sprite);
        if (!TryComp(sprite, out SpriteComponent? comp))
            return depth;

        _sprite.SetDrawDepth((sprite, comp), (int) depth);
        return depth;
    }

    /// <summary>
    ///     This is so the animating entity's current location gets updated during the animation, there is probably a better way to do this.
    /// </summary>
    /// <param name="uid">The entity to update the location of.</param>
    public void UpdatePosition(EntityUid uid)
    {
        var oldPos = _transform.GetWorldPosition(uid);

        // Reset the sprite offset when the entity is in NullSpace
        if (Transform(uid).MapID == MapId.Nullspace)
        {
            if (TryComp(uid, out SpriteComponent? sprite))
                _sprite.SetOffset((uid, sprite), new Vector2());

            return;
        }

        var newPos = oldPos with { Y = oldPos.Y + 0.0001f };
        _transform.SetWorldPosition(uid, newPos);
    }

    public override void Update(float frameTime)
    {
        UpdateColors();
        UpdatePositions();
        UpdateLocalDrawDepth();
    }

    private void UpdateColors()
    {
        try
        {
            var colors = EntityQueryEnumerator<SpriteColorComponent, SpriteComponent>();
            while (colors.MoveNext(out var uid, out var color, out var sprite))
            {
                _sprite.SetColor((uid, sprite), color.Color);
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error updating {nameof(SpriteColorComponent)} colors:\n{e}");
        }
    }

    private void UpdatePositions()
    {
        try
        {
            var location = EntityQueryEnumerator<RMCUpdateClientLocationComponent>();
            while (location.MoveNext(out var uid, out _))
            {
                UpdatePosition(uid);
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error updating {nameof(RMCUpdateClientLocationComponent)} positions:\n{e}");
        }
    }

    private void UpdateLocalDrawDepth()
    {
        try
        {
            if (_player.LocalEntity is not { } player)
                return;

            if (HasComp<GhostComponent>(player))
                return;

            if (TryComp(player, out XenoHideComponent? hide) && hide.Hiding)
                return;

            if (TryComp(player, out SpriteComponent? playerSprite) &&
                !HasComp<ParaDroppingComponent>(player) &&
                !HasComp<CrashLandingComponent>(player))
            {
                _sprite.SetDrawDepth((player, playerSprite), (int) Shared.DrawDepth.DrawDepth.BelowMobs);
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error updating local draw depth:\n{e}");
        }
    }
}
