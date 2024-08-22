using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared._RMC14.Wieldable.Events;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed partial class AttachableModifiersSystem : EntitySystem
{
    private void InitializeWieldDelay()
    {
        SubscribeLocalEvent<AttachableWieldDelayModsComponent, AttachableGetExamineDataEvent>(OnWieldDelayModsGetExamineData);
        SubscribeLocalEvent<AttachableWieldDelayModsComponent, AttachableAlteredEvent>(OnAttachableAltered);
        SubscribeLocalEvent<AttachableWieldDelayModsComponent, AttachableRelayedEvent<GetWieldDelayEvent>>(OnGetWieldDelay);
    }

    private void OnWieldDelayModsGetExamineData(Entity<AttachableWieldDelayModsComponent> attachable, ref AttachableGetExamineDataEvent args)
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

    private List<string> GetEffectStrings(AttachableWieldDelayModifierSet modSet)
    {
        var result = new List<string>();

        if (modSet.Delay != TimeSpan.Zero)
            result.Add(Loc.GetString("rmc-attachable-examine-wield-delay",
                ("colour", modifierExamineColour), ("sign", modSet.Delay.TotalSeconds > 0 ? '+' : ""), ("delay", modSet.Delay.TotalSeconds)));

        return result;
    }

    private void OnAttachableAltered(Entity<AttachableWieldDelayModsComponent> attachable, ref AttachableAlteredEvent args)
    {
        switch(args.Alteration)
        {
            case AttachableAlteredType.AppearanceChanged:
                break;

            case AttachableAlteredType.DetachedDeactivated:
                break;

            case AttachableAlteredType.Wielded:
                break;

            case AttachableAlteredType.Unwielded:
                break;

            default:
                _wieldableSystem.RefreshWieldDelay(args.Holder);
                break;
        }
    }

    private void OnGetWieldDelay(Entity<AttachableWieldDelayModsComponent> attachable, ref AttachableRelayedEvent<GetWieldDelayEvent> args)
    {
        foreach(var modSet in attachable.Comp.Modifiers)
        {
            ApplyModifierSet(attachable, modSet, ref args.Args);
        }
    }

    private void ApplyModifierSet(Entity<AttachableWieldDelayModsComponent> attachable, AttachableWieldDelayModifierSet modSet, ref GetWieldDelayEvent args)
    {
        if (!CanApplyModifiers(attachable.Owner, modSet.Conditions))
            return;

        args.Delay += modSet.Delay;
    }
}
