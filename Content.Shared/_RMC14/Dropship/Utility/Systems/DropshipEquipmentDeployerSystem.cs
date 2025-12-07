using System.Numerics;
using Content.Shared._RMC14.Dropship.AttachmentPoint;
using Content.Shared._RMC14.Dropship.Utility.Components;
using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared.Interaction;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Dropship.Utility.Systems;

public sealed partial class DropshipEquipmentDeployerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DropshipEquipmentDeployerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DropshipEquipmentDeployerComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<DropshipEquipmentDeployerComponent, EntGotInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<DropshipEquipmentDeployerComponent, EntGotRemovedFromContainerMessage>(OnRemovedFromContainer);
    }

    private void OnMapInit(Entity<DropshipEquipmentDeployerComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.DeployPrototype == null)
            return;

        var container = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.DeploySlotId);

        if (container.ContainedEntities.Count > 0)
            return;

        ent.Comp.DeployEntity = GetNetEntity(SpawnInContainerOrDrop(ent.Comp.DeployPrototype, ent, ent.Comp.DeploySlotId));
        Dirty(ent);
    }

    private void OnInteract(Entity<DropshipEquipmentDeployerComponent> ent, ref InteractHandEvent args)
    {
        var parent = Transform(ent).ParentUid;

        if (!TryComp(parent, out DropshipWeaponPointComponent? weaponPoint) &&
            !TryComp(parent, out DropshipUtilityPointComponent? utilityPoint) ||
            ent.Comp.DeployEntity == null)
            return;

        var deployingEntity = GetEntity(ent.Comp.DeployEntity.Value);

        if (_container.TryGetContainer(ent, ent.Comp.DeploySlotId, out var container))
        {
            var deployOffset = Vector2.Zero;
            float rotationOffset = 0;

            if (container.ContainedEntities.Count > 0 && weaponPoint != null)
            {
                switch (weaponPoint.Location)
                {
                    case DropshipWeaponPointLocation.PortFore:
                        deployOffset = ent.Comp.PortForeDeployDirection;
                        rotationOffset = ent.Comp.ForeDeployRotationDegrees;
                        break;
                    case DropshipWeaponPointLocation.PortWing:
                        deployOffset = ent.Comp.PortWingDeployDirection;
                        rotationOffset = ent.Comp.PortWingDeployRotationDegrees;
                        break;
                    case DropshipWeaponPointLocation.StarboardFore:
                        deployOffset = ent.Comp.StarboardForeDeployDirection;
                        rotationOffset = ent.Comp.ForeDeployRotationDegrees;
                        break;
                    case DropshipWeaponPointLocation.StarboardWing:
                        deployOffset = ent.Comp.StarboardWingDeployDirection;
                        rotationOffset = ent.Comp.StarboardWingDeployRotationDegrees;
                        break;
                    case null:
                        break;
                }
            }
            else if (ent.Comp.DeployEntity != null && container.ContainedEntities.Count == 0)
            {
                _container.Insert(deployingEntity, container);
                UpdateAppearance(ent, false);
                return;
            }

            if (!ent.Comp.IsDeployable)
                return;

            _container.EmptyContainer(container, false, Transform(ent).Coordinates.Offset(deployOffset));
            UpdateAppearance(ent, true);

            if (ent.Comp.DeployEntity != null)
                Transform(deployingEntity).LocalRotation += Angle.FromDegrees(rotationOffset);
        }
    }

    private void OnInserted(Entity<DropshipEquipmentDeployerComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (!HasComp<DropshipWeaponPointComponent>(args.Container.Owner) && !HasComp<DropshipUtilityPointComponent>(args.Container.Owner))
        {
            ent.Comp.IsDeployable = false;
            Dirty(ent);
            return;
        }

        ent.Comp.IsDeployable = true;
        Dirty(ent);
    }

    private void OnRemovedFromContainer(Entity<DropshipEquipmentDeployerComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (ent.Comp.DeployEntity != null && _container.TryGetContainer(ent, ent.Comp.DeploySlotId, out var container))
        {
            _container.Insert(GetEntity(ent.Comp.DeployEntity.Value), container);
        }

        ent.Comp.IsDeployable = false;
        Dirty(ent);
    }

    private void UpdateAppearance(Entity<DropshipEquipmentDeployerComponent> ent, bool deployed)
    {
        var parent = Transform(ent).ParentUid;
        var isWeaponPoint = HasComp<DropshipWeaponPointComponent>(parent);
        var isUtilityPoint = HasComp<DropshipUtilityPointComponent>(parent);

        if (!isWeaponPoint && !isUtilityPoint)
            return;

        if (ent.Comp.UtilityDeployedSprite is not { } baseRsi)
            return;

        var rsiPath = baseRsi.RsiPath.ToString();
        var rsiState = baseRsi.RsiState;

        // Undeployed sprites
        if (!deployed && TryComp(ent, out DropshipAttachedSpriteComponent? attached))
        {
            if (isWeaponPoint && attached.WeaponSlotSprite is {} weaponAttachRsi)
            {
                rsiPath = weaponAttachRsi.RsiPath.ToString();
                rsiState = weaponAttachRsi.RsiState;
            }
            else if (isUtilityPoint && attached.Sprite is {} utilityAttachRsi)
            {
                rsiPath = utilityAttachRsi.RsiPath.ToString();
                rsiState = utilityAttachRsi.RsiState;
            }
        }
        // Deployed sprites
        else if (deployed && isWeaponPoint && ent.Comp.WeaponDeployedSprite is {} weaponDeployRsi)
        {
            rsiPath = weaponDeployRsi.RsiPath.ToString();
            rsiState = weaponDeployRsi.RsiState;
        }

        if (isWeaponPoint)
        {
            _appearance.SetData(parent, DropshipWeaponVisuals.Sprite, rsiPath);
            _appearance.SetData(parent, DropshipWeaponVisuals.State, rsiState);
        }
        else
        {
            _appearance.SetData(parent, DropshipUtilityVisuals.Sprite, rsiPath);
            _appearance.SetData(parent, DropshipUtilityVisuals.State, rsiState);
        }
    }
}
