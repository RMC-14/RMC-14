using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared.Item;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed partial class AttachableModifiersSystem : EntitySystem
{
    private readonly List<ItemSizePrototype> _sortedSizes = new();

    private void InitializeSize()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        SubscribeLocalEvent<AttachableSizeModsComponent, AttachableAlteredEvent>(OnAttachableAltered);

        InitItemSizes();
    }

    private void OnAttachableAltered(Entity<AttachableSizeModsComponent> attachable,
        ref AttachableAlteredEvent args)
    {
        if (attachable.Comp.Modifiers.Count == 0)
            return;

        if (!TryComp(args.Holder, out ItemComponent? itemComponent))
            return;

        switch (args.Alteration)
        {
            case AttachableAlteredType.AppearanceChanged:
                break;

            case AttachableAlteredType.DetachedDeactivated:
                break;

            case AttachableAlteredType.Detached:
                ResetSize(args.Holder, itemComponent, attachable.Comp.ResetIncrement);
                break;

            default:
                ResetSize(args.Holder, itemComponent, attachable.Comp.ResetIncrement);
                IncrementSize(
                    attachable,
                    args.Holder,
                    itemComponent,
                    attachable.Comp.Modifiers,
                    out attachable.Comp.ResetIncrement);
                break;
        }
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.ByType.ContainsKey(typeof(ItemSizePrototype)) &&
            args.Removed?.ContainsKey(typeof(ItemSizePrototype)) != true)
        {
            return;
        }

        InitItemSizes();
    }

    private void InitItemSizes()
    {
        _sortedSizes.Clear();
        _sortedSizes.AddRange(_prototypeManager.EnumeratePrototypes<ItemSizePrototype>());
        _sortedSizes.Sort();
    }

    private void IncrementSize(
        Entity<AttachableSizeModsComponent> attachable,
        EntityUid holder,
        ItemComponent itemComponent,
        List<AttachableSizeModifierSet> modifiers,
        out int resetIncrement)
    {
        resetIncrement = 0;
        int sizeIncrement = 0;

        foreach (var modSet in modifiers)
        {
            if (!CanApplyModifiers(attachable, modSet.Conditions))
                continue;
            
            sizeIncrement += modSet.Size;
        }

        if (TryGetIncrementedSize(itemComponent.Size, sizeIncrement, out var newSize, out resetIncrement))
            _itemSystem.SetSize(holder, newSize.Value, itemComponent);
    }

    private void ResetSize(EntityUid holder, ItemComponent itemComponent, int resetIncrement)
    {
        if (TryGetIncrementedSize(itemComponent.Size, resetIncrement, out var newSize, out _))
            _itemSystem.SetSize(holder, newSize.Value, itemComponent);
    }

    private bool TryGetIncrementedSize(
        ProtoId<ItemSizePrototype> size,
        int increment,
        [NotNullWhen(true)] out ProtoId<ItemSizePrototype>? newSize,
        out int resetIncrement)
    {
        var sizeIndex = -1;

        for (var index = 0; index < _sortedSizes.Count; index++)
        {
            if (size.ToString().Equals(_sortedSizes[index].ID))
            {
                sizeIndex = index;
                break;
            }
        }

        if (sizeIndex == -1)
        {
            resetIncrement = 0;
            newSize = null;
            return false;
        }

        var newSizeIndex = MathHelper.Clamp(sizeIndex + increment, 0, _sortedSizes.Count - 1);
        resetIncrement = sizeIndex - newSizeIndex;

        newSize = _sortedSizes[newSizeIndex];
        return true;
    }
}
