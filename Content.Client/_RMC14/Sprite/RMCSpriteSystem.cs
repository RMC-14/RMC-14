using Content.Shared._RMC14.Mobs;
using Content.Shared._RMC14.Sprite;
using Content.Shared._RMC14.Xenonids.Hide;
using Content.Shared.Ghost;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client._RMC14.Sprite;

public sealed class RMCSpriteSystem : SharedRMCSpriteSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

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

        comp.DrawDepth = (int) depth;
        return depth;
    }

    public override void Update(float frameTime)
    {
        var colors = EntityQueryEnumerator<SpriteColorComponent, SpriteComponent>();
        while (colors.MoveNext(out var color, out var sprite))
        {
            sprite.Color = color.Color;
        }

        if (_player.LocalEntity is not { } player)
            return;

        if (HasComp<GhostComponent>(player))
            return;

        if (TryComp(player, out XenoHideComponent? hide) && hide.Hiding)
            return;

        if (TryComp(player, out SpriteComponent? playerSprite))
            playerSprite.DrawDepth = (int) Shared.DrawDepth.DrawDepth.BelowMobs;
    }
}
