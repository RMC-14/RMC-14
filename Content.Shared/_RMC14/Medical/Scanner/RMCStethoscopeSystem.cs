using System.Linq;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Medical.Scanner;

public sealed class RMCStethoscopeSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCStethoscopeComponent, InteractUsingEvent>(OnStethoscopeUsed);
        SubscribeLocalEvent< GetVerbsEvent<ExamineVerb> >(OnGlobalStethoscopeExamineVerb, after: new[] { typeof(SharedPopupSystem) });
    }

    // Add the stethoscope examine verb if the user is holding the stethoscope
    private void OnGlobalStethoscopeExamineVerb(ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || HasComp<XenoComponent>(args.Target))
            return;
        // Check if the user is holding a stethoscope
        if (!IsHoldingStethoscope(args.User, out var stethoscope))
            return;
        var examineMarkup = GetStethoscopeResults(args.Target, args.User);
        _examine.AddDetailedExamineVerb(args,
            Comp<RMCStethoscopeComponent>(stethoscope),
            examineMarkup,
            Loc.GetString("rmc-stethoscope-verb-text"),
            "/Textures/_RMC14/Objects/Medical/stethoscope.rsi/icon.png",
            Loc.GetString("rmc-stethoscope-verb-message"));
    }

    private bool IsHoldingStethoscope(EntityUid user, out EntityUid stethoscope)
    {
        stethoscope = EntityUid.Invalid;
        if (!TryComp<HandsComponent>(user, out var hands))
            return false;
        var held = hands.ActiveHandEntity;
        if (held != null && HasComp<RMCStethoscopeComponent>(held.Value))
        {
            stethoscope = held.Value;
            return true;
        }
        return false;
    }

    private void OnStethoscopeUsed(EntityUid stethoscope, RMCStethoscopeComponent comp, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;
        ShowStethoPopup(args.User, args.Target);
        args.Handled = true;
    }

    private void ShowStethoPopup(EntityUid user, EntityUid target)
    {
        var scanResult = GetStethoscopeResults(target, user);
        var popupMessage = Loc.GetString("rmc-stethoscope-verb-use", ("target", Name(target)), ("user", Name(user)));
        _popup.PopupClient(popupMessage, target, user);
        _examine.SendExamineTooltip(user, target, scanResult, false, true);
    }

    private FormattedMessage GetStethoscopeResults(EntityUid target, EntityUid? user = null)
    {
        var msg = new FormattedMessage();
        if (user != null && !_skills.HasSkill(user.Value, "Medical", 2))
        {
            msg.AddMarkupOrThrow(Loc.GetString("rmc-stethoscope-unskilled"));
            return msg;
        }
        var totalHealth = GetPercentHealth(target);
        if (totalHealth == null)
        {
            msg.AddMarkupOrThrow(Loc.GetString("rmc-stethoscope-nothing"));
        }
        else if (totalHealth >= 85.0f)
        {
            msg.AddMarkupOrThrow(Loc.GetString("rmc-stethoscope-normal"));
        }
        else if (totalHealth >= 62.5f)
        {
            msg.AddMarkupOrThrow(Loc.GetString("rmc-stethoscope-raggedy"));
        }
        else if (totalHealth >= 25.0f)
        {
            msg.AddMarkupOrThrow(Loc.GetString("rmc-stethoscope-hyper"));
        }
        else if (totalHealth >= 1.0f)
        {
            msg.AddMarkupOrThrow(Loc.GetString("rmc-stethoscope-irregular"));
        }
        else if (totalHealth >= 0.0f)
        {
            msg.AddMarkupOrThrow(Loc.GetString("rmc-stethoscope-dead"));
        }
        return msg;
    }

    private float? GetPercentHealth(EntityUid target)
    {
        if (!TryComp<DamageableComponent>(target, out var damage) ||
            !TryComp<MobThresholdsComponent>(target, out var thresholds))
        {
            return null;
        }
        var totalDamage = damage.Damage.GetTotal().Float();
        var maxThreshold = thresholds.Thresholds.Count > 0 ? (float)thresholds.Thresholds.Keys.Max() : 100f;
        var healthPercent = 100.0f - MathF.Min(totalDamage / maxThreshold * 100.0f, 100.0f);
        if (healthPercent > 100.0f)
            healthPercent = 100.0f;
        return healthPercent;
    }
}
