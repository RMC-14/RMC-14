using Content.Shared._RMC14.Ghost;
using Content.Shared.Ghost;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client._RMC14.Ghost;
public sealed class RMCVisibleOnlyToGhostsSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Update(float frameTime)
    {
        var local = _player.LocalEntity;

        var visibleQuery = EntityQueryEnumerator<RMCVisibleToGhostsOnlyComponent, SpriteComponent>();

        bool isGhost = HasComp<GhostComponent>(local);

        while (visibleQuery.MoveNext(out var uid, out var visible, out var sprite))
        {
            if (!_sprite.LayerMapTryGet((uid, sprite), RMCGhostVisibleOnlyVisualLayers.Base, out var layer, true))
                continue;

            _sprite.LayerSetVisible((uid, sprite), layer, isGhost);
        }
    }
}
