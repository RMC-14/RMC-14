using Content.Shared.Examine;
using Content.Shared.Interaction;
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
        SubscribeLocalEvent<RMCStethoscopeComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<RMCStethoscopeComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<RMCStethoscopeComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
    }

    private void OnGetExamineVerbs(EntityUid uid, RMCStethoscopeComponent comp, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;
        if (!IsInHandOrWornBy(args.User, uid))
            return;
        var scanResult = GetStethoscopeScanResult(args.User, args.Target);
        _examine.AddDetailedExamineVerb(args, comp, scanResult,
            Loc.GetString("stethoscope-verb-examine"),
            "/Textures/Interface/VerbIcons/outfit.svg",
            Loc.GetString("stethoscope-verb-examine-tooltip"));
    }

    private bool IsInHandOrWornBy(EntityUid user, EntityUid stethoscope)
    {
        // TODO: Implement logic to check if stethoscope is in user's hand or worn (neck slot)
        // For now, always return true for demonstration
        return true;
    }

    private FormattedMessage GetStethoscopeScanResult(EntityUid user, EntityUid target)
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

    private int GetTotalHealth(EntityUid target)
    {
        // TODO: Replace with actual health component logic
        // For demonstration, return a placeholder value
        // Example: if (TryComp<DamageableComponent>(target, out var damage)) { ... }
        return 100; // Placeholder: always healthy
    }

    private void OnExamined(EntityUid uid, RMCStethoscopeComponent comp, ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
        {
            ShowScanMessageBox(args.Examiner, args.Examined);
        }
    }

    private void OnInteractUsing(EntityUid uid, RMCStethoscopeComponent comp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;
        ShowScanPopup(args.User, args.Target);
        args.Handled = true;
    }

    private void ShowScanMessageBox(EntityUid user, EntityUid target)
    {
        var scanResult = GetStethoscopeScanResult(user, target);
        _examine.AddDetailedExamineVerb(
            new GetVerbsEvent<ExamineVerb>(target, user, true, true),
            null!, // component is not needed for direct message box
            scanResult,
            Loc.GetString("stethoscope-verb-examine"),
            "/Textures/Interface/VerbIcons/outfit.svg",
            Loc.GetString("stethoscope-verb-examine-tooltip"));
    }

    private void ShowScanPopup(EntityUid user, EntityUid target)
    {
        var scanResult = GetStethoscopeScanResult(user, target);
        _popup.PopupClient(scanResult.ToMarkup(), target, user);
    }
}
