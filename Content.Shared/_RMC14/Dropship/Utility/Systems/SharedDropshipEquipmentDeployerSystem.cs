using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared._RMC14.Dropship.AttachmentPoint;
using Content.Shared._RMC14.Dropship.Utility.Components;
using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared._RMC14.Emplacements;
using Content.Shared._RMC14.Sentry;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Dropship.Utility.Systems;

public abstract partial class SharedDropshipEquipmentDeployerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SentrySystem _sentry = default!;
    [Dependency] private readonly SharedWeaponMountSystem _weaponMount = default!;

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

        if (_container.TryGetContainer(ent, ent.Comp.DeploySlotId, out var container))
        {
            var deployOffset = Vector2.Zero;
            var rotationOffset = 0f;

            if (container.ContainedEntities.Count > 0 && weaponPoint != null)
            {
                TryGetOffset(ent, out deployOffset, out rotationOffset, weaponPoint.Location);
            }
            else if (ent.Comp.DeployEntity != null && container.ContainedEntities.Count == 0)
            {
                TryDeploy(ent, false, user: args.User);
                return;
            }

            TryDeploy(ent, true, deployOffset, rotationOffset, user: args.User);
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

        if (HasComp<DropshipWeaponPointComponent>(args.Container.Owner))
            ent.Comp.AutoUnDeploy = true;

        ent.Comp.IsDeployable = true;
        Dirty(ent);
    }

    private void OnRemovedFromContainer(Entity<DropshipEquipmentDeployerComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (ent.Comp.DeployEntity != null)
        {
            TryDeploy(ent,  false);
        }

        ent.Comp.IsDeployable = false;
        ent.Comp.IsDeployed = false;
        ent.Comp.AutoDeploy = false;
        ent.Comp.AutoUnDeploy = false;
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

        ent.Comp.IsDeployed = deployed;
        Dirty(ent);
    }

    /// <summary>
    ///     Try to deploy the equipment stored in the deployer.
    /// </summary>
    /// <param name="deployer">The deployer entity</param>
    /// <param name="deploy">Whether the deployable should be deployed or undeployed</param>
    /// <param name="deployOffset">The position offset of the deployed entity.</param>
    /// <param name="rotationOffset">The rotation offset of the deployed entity.</param>
    /// <param name="equipmentDeployerComponent">The <see cref="DropshipEquipmentDeployerComponent"/> of the deployer</param>
    /// <returns>True if deploying succeeds</returns>
    public bool TryDeploy(EntityUid deployer, bool deploy, Vector2 deployOffset = new (), float rotationOffset = 0, DropshipEquipmentDeployerComponent? equipmentDeployerComponent = null, EntityUid? user = null)
    {
        if (TerminatingOrDeleted(deployer))
            return false;

        if (!Resolve(deployer, ref equipmentDeployerComponent, false))
            return false;

        if (!_container.TryGetContainer(deployer, equipmentDeployerComponent.DeploySlotId, out var container))
            return false;

        if (!equipmentDeployerComponent.IsDeployable)
            return false;

        var deployingEntity = GetEntity(equipmentDeployerComponent.DeployEntity);
        if (deployingEntity != null)
        {
            if (TryComp(deployingEntity, out StrapComponent? strap))
            {
                foreach (var strapped in strap.BuckledEntities)
                {
                    if (TryComp(strapped, out BuckleComponent? buckle))
                        _buckle.Unbuckle((strapped, buckle), strapped);
                }
            }

            if (!deploy)
                _container.Insert(deployingEntity.Value, container);
            else
            {
                _container.EmptyContainer(container, false, Transform(deployer).Coordinates.Offset(deployOffset));
                if (equipmentDeployerComponent.DeployEntity != null)
                    Transform(deployingEntity.Value).LocalRotation += Angle.FromDegrees(rotationOffset);
            }
        }

        UpdateAppearance((deployer, equipmentDeployerComponent), deploy);

        var audio = deploy
            ? equipmentDeployerComponent.DeployAudio
            : equipmentDeployerComponent.UnDeployAudio;

        _audio.PlayPredicted(audio, Transform(deployer).Coordinates, user);
        return true;
    }

    /// <summary>
    ///     Gets the deploy offset based on the slot the deployer is placed in.
    /// </summary>
    /// <param name="deployer">The deployer deploying the entity.</param>
    /// <param name="deployOffset">The position offset.</param>
    /// <param name="rotationOffset">The rotation offset.</param>
    /// <param name="location">The location of the weapon point.</param>
    /// <param name="equipmentDeployerComponent">The <see cref="DropshipEquipmentDeployerComponent"/> of the deployer</param>
    /// <returns>True if an offset is found based on the given location</returns>
    public bool TryGetOffset(EntityUid deployer, out Vector2 deployOffset, out float rotationOffset, DropshipWeaponPointLocation? location = null, DropshipEquipmentDeployerComponent? equipmentDeployerComponent = null)
    {
        deployOffset = Vector2.Zero;
        rotationOffset = 0;

        if (!Resolve(deployer, ref equipmentDeployerComponent, false))
            return false;

        switch (location)
        {
            case DropshipWeaponPointLocation.PortFore:
                deployOffset = equipmentDeployerComponent.PortForeDeployDirection;
                rotationOffset = equipmentDeployerComponent.ForeDeployRotationDegrees;
                return true;
            case DropshipWeaponPointLocation.PortWing:
                deployOffset = equipmentDeployerComponent.PortWingDeployDirection;
                rotationOffset = equipmentDeployerComponent.PortWingDeployRotationDegrees;
                return true;
            case DropshipWeaponPointLocation.StarboardFore:
                deployOffset = equipmentDeployerComponent.StarboardForeDeployDirection;
                rotationOffset = equipmentDeployerComponent.ForeDeployRotationDegrees;
                return true;
            case DropshipWeaponPointLocation.StarboardWing:
                deployOffset = equipmentDeployerComponent.StarboardWingDeployDirection;
                rotationOffset = equipmentDeployerComponent.StarboardWingDeployRotationDegrees;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    ///     Toggles the auto deploy.
    /// </summary>
    /// <param name="deployer">The deployer entity.</param>
    /// <param name="autoDeploy">Should auto deploy be toggled on or off.</param>
    /// <param name="equipmentDeployer">The <see cref="DropshipEquipmentDeployerComponent"/> of the deployer</param>
    public void SetAutoDeploy(EntityUid deployer, bool autoDeploy, DropshipEquipmentDeployerComponent? equipmentDeployer = null)
    {
        if (!Resolve(deployer, ref equipmentDeployer, false))
            return;

        equipmentDeployer.AutoDeploy = autoDeploy;
        Dirty(deployer, equipmentDeployer);
    }

    /// <summary>
    ///     Tries to get the container the deployer is stored inside of.
    /// </summary>
    /// <param name="attachPoint">The entity containing the deployer.</param>
    /// <param name="container">The container containing the deployer.</param>
    /// <returns>True of a container is found</returns>
    public bool TryGetContainer(EntityUid attachPoint, [NotNullWhen(true)] out BaseContainer? container)
    {
        container = null;

        if (TryComp(attachPoint, out DropshipUtilityPointComponent? utilityPoint))
            _container.TryGetContainer(attachPoint, utilityPoint.UtilitySlotId, out container);

        if (TryComp(attachPoint, out DropshipWeaponPointComponent? weaponPoint))
            _container.TryGetContainer(attachPoint, weaponPoint.WeaponContainerSlotId, out container);

        return container != null;
    }

    /// <summary>
    ///     Attempts to get the current and max ammo count of the entity inside a deployer.
    /// </summary>
    /// <param name="deployed">The deployed entity</param>
    /// <param name="ammoCount">The current amount of ammo</param>
    /// <param name="ammoCapacity">The maximum amount of ammo</param>
    /// <returns>True if an ammo count is found</returns>
    public bool TryGetDeployedAmmo(EntityUid deployed, [NotNullWhen(true)] out int? ammoCount, [NotNullWhen(true)] out int? ammoCapacity)
    {
        ammoCount = null;
        ammoCapacity = null;

        return _weaponMount.TryGetWeaponAmmo(deployed, out ammoCount, out ammoCapacity) ||
               _sentry.TryGetSentryAmmo(deployed, out ammoCount, out ammoCapacity);
    }

    /// <summary>
    ///     Tries to get the damage stored on the deployed entity.
    /// </summary>
    /// <param name="deployed">The deployed entity</param>
    /// <param name="damage">The amount of damage on the entity</param>
    /// <returns>True if the entity is damaged</returns>
    public bool TryGetDeployedDamage(EntityUid deployed, out FixedPoint2 damage)
    {
        damage = 0;
        if (!TryComp(deployed, out DamageableComponent? damageable))
            return false;

        if (damageable.TotalDamage <= 0)
            return false;

        damage = damageable.TotalDamage;
        return true;
    }
}
