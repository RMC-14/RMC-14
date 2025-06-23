using System.Linq;
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

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCStethoscopeComponent, InteractUsingEvent>(OnStethoscopeTarget);
        SubscribeLocalEvent<RMCStethoscopeComponent, GetVerbsEvent<ExamineVerb>>(OnStethoscopeVerbExamine);
    }

    private void OnStethoscopeVerbExamine(EntityUid uid, RMCStethoscopeComponent comp, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || HasComp<XenoComponent>(uid))
            return;

        var examineMarkup = GetStethoscopeResults(args.Target);

        _examine.AddDetailedExamineVerb(args,
            component,
            examineMarkup,
            Loc.GetString("rmc-stethoscope-verb-text"),
            "/Textures/Interface/VerbIcons/outfit.svg",
            Loc.GetString("rmc-stethoscope-verb-message"));
    }

    private void OnStethoscopeTarget(EntityUid uid, RMCStethoscopeComponent comp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;
        ShowStethoPopup(args.User, args.Target);
        args.Handled = true;
    }

    private void ShowStethoPopup(EntityUid user, EntityUid target)
    {
        var scanResult = GetStethoscopeResults(target);
        _popup.PopupClient(scanResult.ToMarkup(), target, user);
    }

    private FormattedMessage GetStethoscopeResults(EntityUid target)
    {
        var totalHealth = GetTotalHealth(target);
        var msg = new FormattedMessage();
        if (totalHealth >= 90)
            msg.AddMarkupOrThrow("[color=green]The patient appears to be in excellent health.[/color]");
        else if (totalHealth >= 60)
            msg.AddMarkupOrThrow("[color=yellow]The patient has some minor injuries.[/color]");
        else if (totalHealth >= 30)
            msg.AddMarkupOrThrow("[color=orange]The patient is in poor condition.[/color]");
        else
            msg.AddMarkupOrThrow("[color=red]The patient is in critical condition![/color]");
        return msg;
    }

    private int GetTotalHealth(EntityUid target)//AAAAAAAAAAAAA
    {
        // Try to get the damage and thresholds components
        if (!TryComp<DamageableComponent>(target, out var damage) ||
            !TryComp<MobThresholdsComponent>(target, out var thresholds))
        {
            // If missing, assume healthy
            return 100;
        }

        // Calculate total damage
        var totalDamage = damage.Damage.Total;
        // Find the highest threshold
        var maxThreshold = thresholds.Thresholds.Count > 0 ? (float)thresholds.Thresholds.Keys.Max() : 100f;
        // Clamp and invert to get a health percentage
        var healthPercent = 100 - (int)MathF.Min((float)totalDamage / maxThreshold * 100, 100);
        return healthPercent;
    }
}
