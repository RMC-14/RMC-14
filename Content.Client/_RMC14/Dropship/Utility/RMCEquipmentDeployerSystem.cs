using Content.Shared._RMC14.Dropship.Utility.Components;
using Content.Shared._RMC14.Dropship.Utility.Systems;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Dropship.Utility;

public sealed class RMCEquipmentDeployerSystem : SharedRMCEquipmentDeployerSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCEquipmentDeployerComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<RMCEquipmentDeployerComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnHandleState(Entity<RMCEquipmentDeployerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisuals(ent);
    }

    private void OnAppearanceChange(Entity<RMCEquipmentDeployerComponent> ent, ref AppearanceChangeEvent args)
    {
        UpdateVisuals(ent);
    }

    private void UpdateVisuals(Entity<RMCEquipmentDeployerComponent> ent)
    {
        var deployerEntity = ent.Owner;

        if (!_sprite.LayerMapTryGet(deployerEntity, EquipmentDeployState.UnDeployed, out var deployer, false))
            return;

        if (!_sprite.LayerMapTryGet(deployerEntity, EquipmentDeployState.Deployed, out var deployedEntity, false))
        {
            _sprite.LayerSetVisible(deployerEntity, deployer, true);
            return;
        }

        _sprite.LayerSetVisible(deployerEntity, deployer, !ent.Comp.IsDeployed);
        _sprite.LayerSetVisible(deployerEntity, deployedEntity, ent.Comp.IsDeployed);
    }
}
