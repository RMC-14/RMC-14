using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Humanoid;
using Content.Shared._RMC14.Item;
using Content.Shared._RMC14.Synth;
using Content.Shared._RMC14.UniformAccessories;
using Content.Shared._RMC14.Webbing;
using Content.Shared._RMC14.Xenonids.Damage;
using Content.Shared.Atmos.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DisplacementMap;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Ghost;

public sealed class RMCGhostAppearanceSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ItemCamouflageSystem _itemCamouflage = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;

    private static readonly ProtoId<DamageGroupPrototype> SynthDamageColorGroup = "Brute";

    public void CopyDeathAppearance(EntityUid source, EntityUid ghost)
    {
        CopyOverlayAppearance(source, ghost);

        if (!TryComp(source, out HumanoidAppearanceComponent? sourceHumanoid))
        {
            TryCopyNonHumanoidDeathAppearance(source, ghost);
            return;
        }

        var ghostAppearance = EnsureComp<GhostHumanoidAppearanceComponent>(ghost);
        ghostAppearance.Appearance = SnapshotHumanoidAppearance(sourceHumanoid);
        ghostAppearance.Clothing.Clear();
        ghostAppearance.HeldItems.Clear();

        if (TryComp(source, out InventoryComponent? sourceInventory))
            AppendGhostClothing(source, sourceInventory, sourceHumanoid, ghostAppearance);

        if (TryComp(source, out HandsComponent? sourceHands))
            AppendGhostHeldItems(source, sourceHands, ghostAppearance);

        Dirty(ghost, ghostAppearance);
    }

    private void CopyOverlayAppearance(EntityUid source, EntityUid ghost)
    {
        if (Prototype(source) is not { } prototype)
            return;

        var fire = SnapshotFire(source);
        var damage = HasComp<HumanoidAppearanceComponent>(source) ? SnapshotDamage(source) : null;
        var xenoWound = SnapshotXenoWound(source);
        if (fire == null && damage == null && xenoWound == null)
            return;

        var overlay = EnsureComp<GhostOverlayAppearanceComponent>(ghost);
        overlay.SourcePrototype = prototype.ID;
        overlay.Fire = fire;
        overlay.Damage = damage;
        overlay.XenoWound = xenoWound;
        Dirty(ghost, overlay);
    }

    private GhostFireSnapshot? SnapshotFire(EntityUid source)
    {
        if (!TryComp(source, out FlammableComponent? flammable) || !flammable.OnFire)
            return null;

        return new GhostFireSnapshot
        {
            FireStacks = flammable.FireStacks,
            Color = CompOrNull<RMCFireColorComponent>(source)?.Color,
        };
    }

    private GhostDamageSnapshot? SnapshotDamage(EntityUid source)
    {
        if (!TryComp(source, out DamageableComponent? damageable) || damageable.TotalDamage <= 0)
            return null;

        var snapshot = new GhostDamageSnapshot();
        foreach (var (group, amount) in damageable.DamagePerGroup)
        {
            snapshot.DamagePerGroup[group] = amount;
        }

        if (TryComp(source, out SynthComponent? synth))
            snapshot.GroupColors[SynthDamageColorGroup] = Color.FromHex(synth.DamageVisualsColor);

        return snapshot;
    }

    private GhostXenoWoundSnapshot? SnapshotXenoWound(EntityUid source)
    {
        if (!TryComp(source, out RMCXenoDamageVisualsComponent? visuals) ||
            !_appearance.TryGetData(source, RMCDamageVisuals.State, out int level) ||
            level <= 0)
        {
            return null;
        }

        return new GhostXenoWoundSnapshot
        {
            Prefix = visuals.Prefix,
            States = visuals.States,
            Level = level,
        };
    }

    private void AppendGhostClothing(
        EntityUid source,
        InventoryComponent sourceInventory,
        HumanoidAppearanceComponent sourceHumanoid,
        GhostHumanoidAppearanceComponent ghostAppearance)
    {
        var slotEnumerator = _inventory.GetSlotEnumerator((source, sourceInventory));
        while (slotEnumerator.NextItem(out var item, out var slot))
        {
            if (!TryComp(item, out ClothingComponent? clothing))
                continue;

            var snapshot = new GhostClothingSnapshot
            {
                Slot = slot.Name,
                SlotOffset = slot.Offset,
                Displacement = CopyDisplacement(GetClothingDisplacement(sourceInventory, sourceHumanoid, slot.Name)),
                EquippedPrefix = clothing.EquippedPrefix,
                EquippedState = clothing.EquippedState,
            };

            if (Prototype(item)?.ID is { } protoId)
                snapshot.PrototypeId = protoId;

            if (TryComp(item, out ItemCamouflageComponent? camouflage) &&
                camouflage.CamouflageVariations != null &&
                camouflage.CamouflageVariations.TryGetValue(_itemCamouflage.CurrentMapCamouflage, out var camoClothingRsi))
            {
                snapshot.ClothingRsiPath = camoClothingRsi;
            }

            AppendAccessorySnapshots(item, slot.Name, snapshot);
            AppendWebbingSnapshot(item, slot.Name, snapshot);

            ghostAppearance.Clothing.Add(snapshot);
        }
    }

    private void AppendGhostHeldItems(
        EntityUid source,
        HandsComponent sourceHands,
        GhostHumanoidAppearanceComponent ghostAppearance)
    {
        foreach (var handName in sourceHands.SortedHands)
        {
            if (!_hands.TryGetHeldItem((source, sourceHands), handName, out var held) ||
                !_hands.TryGetHand((source, sourceHands), handName, out var hand))
            {
                continue;
            }

            EntProtoId? protoId = null;
            if (Prototype(held.Value)?.ID is { } id)
                protoId = id;

            string? heldPrefix = null;
            ResPath? itemRsiPath = null;
            if (TryComp(held.Value, out ItemComponent? heldItem))
            {
                heldPrefix = heldItem.HeldPrefix;
            }

            if (TryComp(held.Value, out ItemCamouflageComponent? heldCamouflage) &&
                heldCamouflage.CamouflageVariations != null &&
                heldCamouflage.CamouflageVariations.TryGetValue(_itemCamouflage.CurrentMapCamouflage, out var camoItemRsi))
            {
                itemRsiPath = camoItemRsi;
            }

            ghostAppearance.HeldItems.Add(new GhostHeldItemSnapshot
            {
                PrototypeId = protoId,
                Location = hand.Value.Location,
                Displacement = CopyDisplacement(GetHandDisplacement(sourceHands, hand.Value.Location)),
                HeldPrefix = heldPrefix,
                ItemRsiPath = itemRsiPath,
            });
        }
    }

    private void AppendAccessorySnapshots(EntityUid clothingUid, string slot, GhostClothingSnapshot clothing)
    {
        if (!TryComp<UniformAccessoryHolderComponent>(clothingUid, out var holder) ||
            !_container.TryGetContainer(clothingUid, holder.ContainerId, out var container))
        {
            return;
        }

        var index = 0;
        foreach (var accessory in container.ContainedEntities)
        {
            if (!TryComp<UniformAccessoryComponent>(accessory, out var comp))
            {
                index++;
                continue;
            }

            if (holder.HideAccessories && comp.HiddenByJacketRolling)
            {
                index++;
                continue;
            }

            if (comp.PlayerSprite is not { } sprite)
            {
                index++;
                continue;
            }

            var bookmarkKey = comp.LayerKeys is { Count: > 0 } ? null : slot;

            clothing.Accessories.Add(new GhostAccessorySnapshot
            {
                Sprite = sprite.RsiPath,
                State = sprite.RsiState,
                Visible = !comp.Hidden,
                LayerKey = GetUniformAccessoryKey(accessory, comp, index),
                BookmarkKey = bookmarkKey,
            });

            index++;
        }
    }

    private void AppendWebbingSnapshot(EntityUid clothingUid, string slot, GhostClothingSnapshot clothing)
    {
        if (!TryComp<WebbingClothingComponent>(clothingUid, out var clothingComp) ||
            clothingComp.Webbing is not { } webbingUid ||
            !TryComp<WebbingComponent>(webbingUid, out var webbing) ||
            webbing.PlayerSprite is not { } sprite)
        {
            return;
        }

        var isOuter = clothingComp.Whitelist?.Tags?.Contains("ArmorWebbing") == true;
        clothing.Webbing = new GhostWebbingSnapshot
        {
            Sprite = sprite.RsiPath,
            State = sprite.RsiState,
            IsOuter = isOuter,
            BookmarkKey = slot,
        };
    }

    private static RMCHumanoidAppearance SnapshotHumanoidAppearance(HumanoidAppearanceComponent sourceHumanoid)
    {
        return new RMCHumanoidAppearance
        {
            ClientOldMarkings = new(sourceHumanoid.ClientOldMarkings),
            MarkingSet = new(sourceHumanoid.MarkingSet),
            BaseLayers = new(sourceHumanoid.BaseLayers),
            PermanentlyHidden = new(sourceHumanoid.PermanentlyHidden),
            Gender = sourceHumanoid.Gender,
            Age = sourceHumanoid.Age,
            CustomBaseLayers = new(sourceHumanoid.CustomBaseLayers),
            Species = sourceHumanoid.Species,
            Initial = sourceHumanoid.Initial,
            SkinColor = sourceHumanoid.SkinColor,
            HiddenLayers = new(sourceHumanoid.HiddenLayers),
            Sex = sourceHumanoid.Sex,
            EyeColor = sourceHumanoid.EyeColor,
            CachedHairColor = sourceHumanoid.CachedHairColor,
            CachedFacialHairColor = sourceHumanoid.CachedFacialHairColor,
            HideLayersOnEquip = new(sourceHumanoid.HideLayersOnEquip),
            UndergarmentTop = sourceHumanoid.UndergarmentTop,
            UndergarmentBottom = sourceHumanoid.UndergarmentBottom,
            MarkingsDisplacement = new(sourceHumanoid.MarkingsDisplacement),
        };
    }

    private static DisplacementData? GetClothingDisplacement(
        InventoryComponent inventory,
        HumanoidAppearanceComponent humanoid,
        string slot)
    {
        return humanoid.Sex switch
        {
            Sex.Male when inventory.MaleDisplacements.Count > 0 => inventory.MaleDisplacements.GetValueOrDefault(slot),
            Sex.Female when inventory.FemaleDisplacements.Count > 0 => inventory.FemaleDisplacements.GetValueOrDefault(slot),
            _ => inventory.Displacements.GetValueOrDefault(slot),
        };
    }

    private static DisplacementData? GetHandDisplacement(HandsComponent hands, HandLocation location)
    {
        return location switch
        {
            HandLocation.Left when hands.LeftHandDisplacement != null => hands.LeftHandDisplacement,
            HandLocation.Middle or HandLocation.Right when hands.RightHandDisplacement != null => hands.RightHandDisplacement,
            _ => hands.HandDisplacement,
        };
    }

    private DisplacementData? CopyDisplacement(DisplacementData? displacement)
    {
        return displacement == null ? null : _serialization.CreateCopy(displacement, notNullableOverride: true);
    }

    private string GetUniformAccessoryKey(EntityUid uid, UniformAccessoryComponent component, int index)
    {
        var key = $"enum.{nameof(UniformAccessoryLayer)}.{UniformAccessoryLayer.Base}{index}_{Name(uid)}_{uid.Id}";

        if (component.LayerKeys != null && component.LayerKeys.Count > 0 && component.Limit > 1)
        {
            var layerIndex = index < component.LayerKeys.Count ? index : component.LayerKeys.Count - 1;
            key = component.LayerKeys[layerIndex];
        }
        else if (component.LayerKeys != null && component.LayerKeys.Count == 1)
        {
            key = component.LayerKeys[0];
        }

        return key;
    }

    private bool TryCopyNonHumanoidDeathAppearance(EntityUid source, EntityUid ghost)
    {
        if (Prototype(source) is not { } prototype)
            return false;

        var ghostAppearance = EnsureComp<GhostNonHumanoidAppearanceComponent>(ghost);
        ghostAppearance.Sprite = null;
        ghostAppearance.State = null;
        ghostAppearance.SourcePrototype = prototype.ID;
        Dirty(ghost, ghostAppearance);
        return true;
    }
}
