using Content.Shared._RMC14.Pointing;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client._RMC14.Pointing;

public sealed class RMCIgnorePointingPointerHideVisualizerSystem : VisualizerSystem<RMCPointingArrowComponent>
{
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCPointingArrowComponent, ComponentStartup>(OnPointSpawn);
    }

    private void OnPointSpawn(Entity<RMCPointingArrowComponent> arrow, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(arrow, out var sprite))
            return;

        var localEntity = _player.LocalEntity;

        if (!TryComp<RMCIgnorePointingComponent>(localEntity, out var ignore) || arrow.Comp.Source == null)
            return;

        var ent = GetEntity(arrow.Comp.Source);

        if (localEntity == ent)
            return;

        if (!((ignore.IgnoreMobs && HasComp<MobStateComponent>(ent)) || (ignore.IgnoreGhosts && HasComp<GhostComponent>(ent))))
            return;

        SpriteSystem.SetVisible((arrow.Owner, sprite), false);
    }
}
