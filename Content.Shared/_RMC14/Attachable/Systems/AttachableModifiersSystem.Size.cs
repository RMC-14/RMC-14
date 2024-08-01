using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared._RMC14.Item;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed partial class AttachableModifiersSystem : EntitySystem
{
    [Dependency] private readonly ItemSizeChangeSystem _itemSizeChangeSystem = default!;

    private void InitializeSize()
    {
        SubscribeLocalEvent<AttachableSizeModsComponent, AttachableGetExamineDataEvent>(OnSizeModsGetExamineData);
        SubscribeLocalEvent<AttachableSizeModsComponent, AttachableAlteredEvent>(OnAttachableAltered);
        SubscribeLocalEvent<AttachableSizeModsComponent, AttachableRelayedEvent<GetItemSizeModifiersEvent>>(OnGetItemSizeModifiers);
    }

    private void OnSizeModsGetExamineData(Entity<AttachableSizeModsComponent> attachable, ref AttachableGetExamineDataEvent args)
    {
        foreach (var modSet in attachable.Comp.Modifiers)
        {
            var key = GetExamineKey(modSet.Conditions);

            if (!args.Data.ContainsKey(key))
                args.Data[key] = new (modSet.Conditions, GetEffectStrings(modSet));
            else
                args.Data[key].effectStrings.AddRange(GetEffectStrings(modSet));
        }
    }

    private List<string> GetEffectStrings(AttachableSizeModifierSet modSet)
    {
        var result = new List<string>();

        if (modSet.Size != 0)
            result.Add(Loc.GetString("rmc-attachable-examine-size",
                ("colour", modifierExamineColour), ("sign", modSet.Size > 0 ? '+' : ""), ("size", modSet.Size)));

        return result;
    }

    private void OnAttachableAltered(Entity<AttachableSizeModsComponent> attachable, ref AttachableAlteredEvent args)
    {
        if (attachable.Comp.Modifiers.Count == 0)
            return;

        switch (args.Alteration)
        {
            case AttachableAlteredType.AppearanceChanged:
                break;

            case AttachableAlteredType.DetachedDeactivated:
                break;

            default:
                _itemSizeChangeSystem.RefreshItemSizeModifiers(args.Holder);
                break;
        }
    }

    private void OnGetItemSizeModifiers(Entity<AttachableSizeModsComponent> attachable, ref AttachableRelayedEvent<GetItemSizeModifiersEvent> args)
    {
        foreach(var modSet in attachable.Comp.Modifiers)
        {
            if (!CanApplyModifiers(attachable.Owner, modSet.Conditions))
                return;

            args.Args.Size += modSet.Size;
        }
    }
}
