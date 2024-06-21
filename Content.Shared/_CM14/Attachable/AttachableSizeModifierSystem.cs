using System.Diagnostics.CodeAnalysis;
using Content.Shared._CM14.Attachable.Components;
using Content.Shared._CM14.Attachable.Events;
using Content.Shared.Item;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Attachable;

public sealed class AttachableSizeModifierSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedItemSystem _itemSystem = default!;

    private readonly List<ItemSizePrototype> _sortedSizes = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        SubscribeLocalEvent<AttachableSizeModifierComponent, AttachableAlteredEvent>(OnAttachableAltered);
        SubscribeLocalEvent<AttachableToggleableSizeModifierComponent, AttachableAlteredEvent>(OnAttachableAltered);

        InitItemSizes();
    }

    private void OnAttachableAltered(Entity<AttachableSizeModifierComponent> attachable,
        ref AttachableAlteredEvent args)
    {
        if (attachable.Comp.SizeModifier == 0)
            return;

        if (!TryComp(args.Holder, out ItemComponent? itemComponent))
            return;

        switch (args.Alteration)
        {
            case AttachableAlteredType.Attached:
                IncrementSize(args.Holder,
                    itemComponent,
                    attachable.Comp.SizeModifier,
                    out attachable.Comp.ResetIncrement);
                return;

            case AttachableAlteredType.Detached:
                ResetSize(args.Holder, itemComponent, attachable.Comp.ResetIncrement);
                return;
        }
    }

    private void OnAttachableAltered(Entity<AttachableToggleableSizeModifierComponent> attachable,
        ref AttachableAlteredEvent args)
    {
        if (attachable.Comp.ActiveSizeModifier == 0 && attachable.Comp.InactiveSizeModifier == 0)
            return;

        if (!TryComp(args.Holder, out ItemComponent? itemComponent))
            return;

        if (!TryComp(attachable.Owner, out AttachableToggleableComponent? toggleableComponent))
            return;

        switch (args.Alteration)
        {
            case AttachableAlteredType.Attached:
                if (toggleableComponent.Active)
                {
                    IncrementSize(args.Holder,
                        itemComponent,
                        attachable.Comp.ActiveSizeModifier,
                        out attachable.Comp.ResetIncrement);
                    return;
                }

                IncrementSize(args.Holder,
                    itemComponent,
                    attachable.Comp.InactiveSizeModifier,
                    out attachable.Comp.ResetIncrement);
                return;

            case AttachableAlteredType.Detached:
                ResetSize(args.Holder, itemComponent, attachable.Comp.ResetIncrement);
                return;

            case AttachableAlteredType.Activated:
                ResetSize(args.Holder, itemComponent, attachable.Comp.ResetIncrement);
                IncrementSize(args.Holder,
                    itemComponent,
                    attachable.Comp.ActiveSizeModifier,
                    out attachable.Comp.ResetIncrement);
                return;

            case AttachableAlteredType.Deactivated:
                ResetSize(args.Holder, itemComponent, attachable.Comp.ResetIncrement);
                IncrementSize(args.Holder,
                    itemComponent,
                    attachable.Comp.InactiveSizeModifier,
                    out attachable.Comp.ResetIncrement);
                return;
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

    private void IncrementSize(EntityUid holder, ItemComponent itemComponent, int sizeIncrement, out int resetIncrement)
    {
        if (TryGetIncrementedSize(itemComponent.Size, sizeIncrement, out var newSize, out resetIncrement))
            _itemSystem.SetSize(holder, newSize.Value, itemComponent);
    }

    private void ResetSize(EntityUid holder, ItemComponent itemComponent, int resetIncrement)
    {
        if (TryGetIncrementedSize(itemComponent.Size, resetIncrement, out var newSize, out _))
            _itemSystem.SetSize(holder, newSize.Value, itemComponent);
    }

    private bool TryGetIncrementedSize(ProtoId<ItemSizePrototype> size,
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
