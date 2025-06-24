using System.Linq;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Damage;
using Content.Shared.Examine;
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
        SubscribeLocalEvent<RMCStethoscopeComponent, InteractUsingEvent>(OnStethoscopeTarget);
        SubscribeLocalEvent<RMCStethoscopeComponent, GetVerbsEvent<ExamineVerb>>(OnStethoscopeVerbExamine);
    }

    private void OnStethoscopeTarget(EntityUid uid, RMCStethoscopeComponent comp, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;
        ShowStethoPopup(args.User, args.Target);
        args.Handled = true;
    }

    private void OnStethoscopeVerbExamine(EntityUid uid, RMCStethoscopeComponent comp, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || HasComp<XenoComponent>(uid))
            return;
        var examineMarkup = GetStethoscopeResults(args.Target);
        _examine.AddDetailedExamineVerb(args,
            comp,
            examineMarkup,
            Loc.GetString("rmc-stethoscope-verb-text"),
            "/Textures/_RMC14/Objects/Medical/stethoscope.rsi/icon.png",
            Loc.GetString("rmc-stethoscope-verb-message"));
    }

    private void ShowStethoPopup(EntityUid user, EntityUid target)
    {
        var scanResult = GetStethoscopeResults(target, user);
        var popupMessage = Loc.GetString("rmc-stethoscope-verb-use", ("target", Name(target)), ("user", Name(user)));
        _popup.PopupClient(popupMessage, target, user);
        _popup.PopupEntity(scanResult.ToString(), target, user);
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
        var possessivePronoun = Loc.GetString("zzzz-possessive-pronoun", ("ent", target));
        var subjectPronoun = Loc.GetString("zzzz-subject-pronoun", ("ent", target));
        if (totalHealth == null)
        {
            msg.AddMarkupOrThrow(Loc.GetString("rmc-stethoscope-nothing", (possessivePronoun, possessivePronoun)));
        }
        else if (totalHealth >= 85.0f)
        {
            msg.AddMarkupOrThrow(Loc.GetString("rmc-stethoscope-normal"));
        }
        else if (totalHealth >= 62.5f)
        {
            msg.AddMarkupOrThrow(Loc.GetString("rmc-stethoscope-raggedy", (subjectPronoun, subjectPronoun)));
        }
        else if (totalHealth >= 25.0f)
        {
            msg.AddMarkupOrThrow(Loc.GetString("rmc-stethoscope-hyper"));
        }
        else if (totalHealth >= 1.0f)
        {
            msg.AddMarkupOrThrow(Loc.GetString("rmc-stethoscope-irregular", (subjectPronoun, subjectPronoun)));
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
