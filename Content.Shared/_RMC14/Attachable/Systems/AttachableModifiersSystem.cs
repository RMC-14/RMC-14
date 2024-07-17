using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared._RMC14.Wieldable;
using Content.Shared.Examine;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Content.Shared.Wieldable.Components;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed partial class AttachableModifiersSystem : EntitySystem
{
    [Dependency] private readonly AttachableHolderSystem _attachableHolderSystem = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly CMGunSystem _cmGunSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly RMCSelectiveFireSystem _rmcSelectiveFireSystem = default!;
    [Dependency] private readonly RMCWieldableSystem _wieldableSystem = default!;

    private const string modifierExamineColour = "yellow";

    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableComponent, ExaminedEvent>(OnAttachableExamine);

        InitializeMelee();
        InitializeRanged();
        InitializeSize();
        InitializeSpeed();
        InitializeWieldDelay();
    }

    private void OnAttachableExamine(Entity<AttachableComponent> attachable, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var ev = new AttachableGetExamineDataEvent(new Dictionary<byte, (AttachableModifierConditions? conditions, List<string> effectStrings)>());
        RaiseLocalEvent(attachable.Owner, ref ev);

        using (args.PushGroup(nameof(AttachableComponent)))
        {
            foreach (var key in ev.Data.Keys)
            {
                args.PushMarkup(GetExamineConditionText(attachable, ev.Data[key].conditions));

                foreach (var effectText in ev.Data[key].effectStrings)
                    args.PushMarkup("    " + effectText);
            }
        }
    }

    private string GetExamineConditionText(Entity<AttachableComponent> attachable, AttachableModifierConditions? conditions)
    {
        string conditionText = Loc.GetString("rmc-attachable-examine-condition-always");

        if (conditions == null)
            return conditionText;

        AttachableModifierConditions cond = conditions.Value;

        bool conditionPlaced = false;
        conditionText = Loc.GetString("rmc-attachable-examine-condition-when") + ' ';

        ExamineConditionAddEntry(cond.WieldedOnly, Loc.GetString("rmc-attachable-examine-condition-wielded"), ref conditionText, ref conditionPlaced);
        ExamineConditionAddEntry(cond.UnwieldedOnly, Loc.GetString("rmc-attachable-examine-condition-unwielded"), ref conditionText, ref conditionPlaced);

        ExamineConditionAddEntry(
            cond.ActiveOnly,
            Loc.GetString("rmc-attachable-examine-condition-active", ("attachable", attachable.Owner)),
            ref conditionText,
            ref conditionPlaced);

        ExamineConditionAddEntry(
            cond.InactiveOnly,
            Loc.GetString("rmc-attachable-examine-condition-inactive", ("attachable", attachable.Owner)),
            ref conditionText,
            ref conditionPlaced);

        if (cond.Whitelist != null && cond.Whitelist.Tags != null)
        {
            EntityWhitelist whitelist = cond.Whitelist;

            ExamineConditionAddEntry(
                cond.Whitelist != null,
                Loc.GetString("rmc-attachable-examine-condition-whitelist", ("tagNumber", whitelist.RequireAll ? "all" : "one"), ("tags", String.Join(", ", whitelist.Tags))),
                ref conditionText,
                ref conditionPlaced);
        }

        if (cond.Blacklist != null && cond.Blacklist.Tags != null)
        {
            EntityWhitelist blacklist = cond.Blacklist;

            ExamineConditionAddEntry(
                cond.Blacklist != null,
                Loc.GetString("rmc-attachable-examine-condition-blacklist", ("tagNumber", blacklist.RequireAll ? "one" : "all"), ("tags", String.Join(", ", blacklist.Tags))),
                ref conditionText,
                ref conditionPlaced);
        }

        conditionText += ':';

        return conditionText;
    }

    private void ExamineConditionAddEntry(bool condition, string text, ref string conditionText, ref bool conditionPlaced)
    {
        if (!condition)
            return;

        if (conditionPlaced)
            conditionText += "; ";
        conditionText += text;
        conditionPlaced = true;
    }

    private byte GetExamineKey(AttachableModifierConditions? conditions)
    {
        byte key = 0;

        if (conditions == null)
            return key;

        key |= conditions.Value.WieldedOnly ? (byte)(1 << 0) : (byte)0;
        key |= conditions.Value.UnwieldedOnly ? (byte)(1 << 1) : (byte)0;
        key |= conditions.Value.ActiveOnly ? (byte)(1 << 2) : (byte)0;
        key |= conditions.Value.InactiveOnly ? (byte)(1 << 3) : (byte)0;
        key |= conditions.Value.Whitelist != null ? (byte)(1 << 4) : (byte)0;
        key |= conditions.Value.Blacklist != null ? (byte)(1 << 5) : (byte)0;

        return key;
    }

    private bool CanApplyModifiers(EntityUid attachableUid, AttachableModifierConditions? conditions)
    {
        if (conditions == null)
            return true;

        _attachableHolderSystem.TryGetHolder(attachableUid, out var holderUid);

        if (holderUid != null)
        {
            TryComp(holderUid, out WieldableComponent? wieldableComponent);

            if (conditions.Value.UnwieldedOnly && wieldableComponent != null && wieldableComponent.Wielded)
                return false;
            else if (conditions.Value.WieldedOnly && (wieldableComponent == null || !wieldableComponent.Wielded))
                return false;
        }

        TryComp(attachableUid, out AttachableToggleableComponent? toggleableComponent);

        if (conditions.Value.InactiveOnly && toggleableComponent != null && toggleableComponent.Active)
            return false;
        else if (conditions.Value.ActiveOnly && (toggleableComponent == null || !toggleableComponent.Active))
            return false;


        if (holderUid != null)
        {
            if (conditions.Value.Whitelist != null && _whitelistSystem.IsWhitelistFail(conditions.Value.Whitelist, holderUid.Value))
                return false;

            if (conditions.Value.Blacklist != null && _whitelistSystem.IsWhitelistPass(conditions.Value.Blacklist, holderUid.Value))
                return false;
        }

        return true;
    }
}
