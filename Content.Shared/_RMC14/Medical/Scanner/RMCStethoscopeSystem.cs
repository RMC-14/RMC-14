using System.Linq;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.UniformAccessories;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Medical.Scanner;

public sealed class RMCStethoscopeSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private static readonly EntProtoId<SkillDefinitionComponent> MedicalSkill = "RMCSkillMedical";
    private static readonly string[] AccessorySlots = ["jumpsuit", "outerClothing"];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GetVerbsEvent<ExamineVerb>>(OnGlobalStethoscopeExamineVerb, after: new[] { typeof(SharedPopupSystem) });
        SubscribeLocalEvent<RMCStethoscopeComponent, AfterInteractEvent>(OnStethoAfterInteract);
    }

    private void OnStethoAfterInteract(EntityUid uid, RMCStethoscopeComponent comp, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;
        if (!HasStethoscope(args.User, out _))
            return;
        if (args.Target == null)
            return;
        ShowStethoPopup(args.User, args.Target.Value);
        args.Handled = true;
    }

    private void ShowStethoPopup(EntityUid user, EntityUid target)
    {
        var scanResult = GetStethoscopeResults(target, user);
        var popupText = scanResult.ToString();
        _popup.PopupClient(popupText, user, user);
    }

    private void OnGlobalStethoscopeExamineVerb(GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || HasComp<XenoComponent>(args.Target))
            return;
        if (!HasStethoscope(args.User, out var stethoscope))
            return;
        var examineMarkup = GetStethoscopeResults(args.Target, args.User);
        _examine.AddDetailedExamineVerb(args,
            Comp<RMCStethoscopeComponent>(stethoscope),
            examineMarkup,
            Loc.GetString("rmc-stethoscope-verb-text"),
            "/Textures/_RMC14/Objects/Medical/stethoscope.rsi/icon.png",
            Loc.GetString("rmc-stethoscope-verb-message"));
    }

    private bool HasStethoscope(EntityUid user, out EntityUid stethoscope)
    {
        stethoscope = EntityUid.Invalid;
        if (_hands.TryGetActiveItem(user, out var held) &&
            HasComp<RMCStethoscopeComponent>(held.Value))
        {
            stethoscope = held.Value;
            return true;
        }

        foreach (var slot in AccessorySlots)
        {
            if (!_inventorySystem.TryGetSlotEntity(user, slot, out var slotEntity))
                continue;
            if (!TryComp<UniformAccessoryHolderComponent>(slotEntity.Value, out var accessoryHolder))
                continue;
            if (!_containerSystem.TryGetContainer(slotEntity.Value, accessoryHolder.ContainerId, out var container))
                continue;
            foreach (var accessory in container.ContainedEntities)
            {
                if (!HasComp<RMCStethoscopeComponent>(accessory))
                    continue;
                stethoscope = accessory;
                return true;
            }
        }

        return false;
    }

    private FormattedMessage GetStethoscopeResults(EntityUid target, EntityUid? user = null)
    {
        var msg = new FormattedMessage();
        if (user != null && !_skills.HasSkill(user.Value, MedicalSkill, 2))
        {
            msg.AddMarkupOrThrow(Loc.GetString("rmc-stethoscope-unskilled"));
            return msg;
        }

        if (_mobState.IsDead(target))
        {
            msg.AddMarkupOrThrow(Loc.GetString("rmc-stethoscope-dead"));
            return msg;
        }

        var totalHealth = GetPercentHealth(target) switch
        {
            null => "rmc-stethoscope-nothing",
            >= 87.5f => "rmc-stethoscope-normal",
            >= 62.5f => "rmc-stethoscope-raggedy",
            >= 37.5f => "rmc-stethoscope-hyper",
            >= 0.1f => "rmc-stethoscope-irregular",
            _ => "rmc-stethoscope-dead"
        };

        var locString = totalHealth is "rmc-stethoscope-nothing" or "rmc-stethoscope-hyper" or "rmc-stethoscope-dead"
            ? Loc.GetString(totalHealth)
            : Loc.GetString(totalHealth, ("target", target));

        msg.AddMarkupOrThrow(locString);
        return msg;
    }

    private float? GetPercentHealth(EntityUid target)
    {
        if (!TryComp<DamageableComponent>(target, out var damageable) ||
            !TryComp<MobThresholdsComponent>(target, out var thresholds))
        {
            return null;
        }

        var totalDamage = damageable.Damage.GetTotal().Float();
        var maxHealthThreshold = thresholds.Thresholds.Count > 0
            ? (float)thresholds.Thresholds.Keys.Max()
            : 100f;
        var damagePercent = totalDamage / maxHealthThreshold * 100.0f;
        var healthPercent = 100.0f - MathF.Min(damagePercent, 100.0f);
        return MathF.Max(healthPercent, 0.0f);
    }
}
