using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared._RMC14.Wieldable;
using Content.Shared._RMC14.Wieldable.Events;
using Content.Shared.Wieldable.Components;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed partial class AttachableModifiersSystem : EntitySystem
{
    private void InitializeSpeed()
    {
        SubscribeLocalEvent<AttachableSpeedModsComponent, AttachableGetExamineDataEvent>(OnSpeedModsGetExamineData);
        SubscribeLocalEvent<AttachableSpeedModsComponent, AttachableAlteredEvent>(OnAttachableAltered);
        SubscribeLocalEvent<AttachableSpeedModsComponent, AttachableRelayedEvent<GetWieldableSpeedModifiersEvent>>(OnGetSpeedModifiers);
    }

    private void OnSpeedModsGetExamineData(Entity<AttachableSpeedModsComponent> attachable, ref AttachableGetExamineDataEvent args)
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

    private List<string> GetEffectStrings(AttachableSpeedModifierSet modSet)
    {
        var result = new List<string>();

        if (modSet.Walk != 0)
            result.Add(Loc.GetString("rmc-attachable-examine-speed-walk",
                ("colour", modifierExamineColour), ("sign", modSet.Walk > 0 ? '+' : ""), ("speed", modSet.Walk)));

        if (modSet.Sprint != 0)
            result.Add(Loc.GetString("rmc-attachable-examine-speed-sprint",
                ("colour", modifierExamineColour), ("sign", modSet.Sprint > 0 ? '+' : ""), ("speed", modSet.Sprint)));

        return result;
    }

    private void OnAttachableAltered(Entity<AttachableSpeedModsComponent> attachable, ref AttachableAlteredEvent args)
    {
        switch(args.Alteration)
        {
            case AttachableAlteredType.AppearanceChanged:
                break;

            case AttachableAlteredType.DetachedDeactivated:
                break;

            default:
                _wieldableSystem.RefreshSpeedModifiers(args.Holder);
                break;
        }
    }

    private void OnGetSpeedModifiers(Entity<AttachableSpeedModsComponent> attachable, ref AttachableRelayedEvent<GetWieldableSpeedModifiersEvent> args)
    {
        foreach(var modSet in attachable.Comp.Modifiers)
        {
            ApplyModifierSet(attachable, modSet, ref args.Args);
        }
    }

    private void ApplyModifierSet(Entity<AttachableSpeedModsComponent> attachable, AttachableSpeedModifierSet modSet, ref GetWieldableSpeedModifiersEvent args)
    {
        if (!CanApplyModifiers(attachable.Owner, modSet.Conditions))
            return;

        args.Walk += modSet.Walk;
        args.Sprint += modSet.Sprint;
    }
}
