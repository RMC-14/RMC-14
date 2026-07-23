using System.Text;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Dialog;

public sealed class DialogSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        Subs.BuiEvents<DialogComponent>(DialogUiKey.Key, subs =>
        {
            subs.Event<DialogOptionBuiMsg>(OnDialogOption);
            subs.Event<DialogInputBuiMsg>(OnDialogInput);
            subs.Event<DialogConfirmBuiMsg>(OnDialogConfirm);
            subs.Event<BoundUIClosedEvent>(OnDialogClosed);
        });
    }

    private void OnDialogOption(Entity<DialogComponent> ent, ref DialogOptionBuiMsg args)
    {
        if (!CanSubmitDialog(ent, args.Actor, args))
            return;

        var target = ent.Comp.EventTarget;
        var index = args.Index;
        if (index < 0 || !ent.Comp.Options.TryGetValue(index, out var option))
            return;

        _ui.CloseUi(ent.Owner, DialogUiKey.Key);

        var ev = new DialogChosenEvent(args.Actor, index);
        RaiseLocalEvent(target, ref ev);

        if (option.Event != null)
            RaiseLocalEvent(target, ref option.Event, true);
    }

    private void OnDialogInput(Entity<DialogComponent> ent, ref DialogInputBuiMsg args)
    {
        if (!CanSubmitDialog(ent, args.Actor, args) ||
            ent.Comp.InputEvent == null)
        {
            return;
        }

        var msg = TrimToLimit(args.Input, ent.Comp.CharacterLimit, ent.Comp.SmartCheck);
        var inputEvent = ent.Comp.InputEvent with { Message = msg };
        var target = ent.Comp.EventTarget;

        _ui.CloseUi(ent.Owner, DialogUiKey.Key);
        RaiseLocalEvent(target, (object) inputEvent);
    }

    private void OnDialogConfirm(Entity<DialogComponent> ent, ref DialogConfirmBuiMsg args)
    {
        if (!CanSubmitDialog(ent, args.Actor, args) ||
            ent.Comp.ConfirmEvent == null)
        {
            return;
        }

        var confirmEvent = ent.Comp.ConfirmEvent;
        var target = ent.Comp.EventTarget;

        _ui.CloseUi(ent.Owner, DialogUiKey.Key);
        RaiseLocalEvent(target, confirmEvent);
    }

    private void OnDialogClosed(Entity<DialogComponent> ent, ref BoundUIClosedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        RemComp<DialogComponent>(ent);
    }

    public void OpenOptions(EntityUid target, EntityUid actor, string title, List<DialogOption> options, string message = "")
    {
        var dialog = PrepareDialog(target, actor);
        dialog.Title = title;
        dialog.Message = new DialogOption(message);
        dialog.DialogType = DialogType.Options;
        dialog.Options = options;
        dialog.InputEvent = null;
        dialog.ConfirmEvent = null;
        Dirty(actor, dialog);

        _ui.TryOpenUi(actor, DialogUiKey.Key, actor);
    }

    public void OpenOptions(EntityUid actor, string title, List<DialogOption> options, string message = "")
    {
        OpenOptions(actor, actor, title, options, message);
    }

    public void OpenInput(EntityUid target, EntityUid actor, string message, DialogInputEvent? ev, bool largeInput = false, int characterLimit = 200, int minCharacterLimit = 0, bool smartCheck = false, bool autoFocus = true)
    {
        var dialog = PrepareDialog(target, actor);
        dialog.DialogType = DialogType.Input;
        dialog.Message = new DialogOption(message, ev);
        dialog.Options.Clear();
        dialog.InputEvent = ev;
        dialog.ConfirmEvent = null;
        dialog.LargeInput = largeInput;
        dialog.CharacterLimit = characterLimit;
        dialog.MinCharacterLimit = minCharacterLimit;
        dialog.SmartCheck = smartCheck;
        dialog.AutoFocus = autoFocus;

        Dirty(actor, dialog);

        _ui.TryOpenUi(actor, DialogUiKey.Key, actor);
    }

    public void OpenInput(EntityUid actor, string message, DialogInputEvent? ev, bool largeInput = false, int characterLimit = 200, int minCharacterLimit = 0, bool smartCheck = false, bool autoFocus = true)
    {
        OpenInput(actor, actor, message, ev, largeInput, characterLimit, minCharacterLimit, smartCheck, autoFocus);
    }

    public void OpenConfirmation(EntityUid target, EntityUid actor, string title, string message, object ev)
    {
        var dialog = PrepareDialog(target, actor);
        dialog.DialogType = DialogType.Confirm;
        dialog.Title = title;
        dialog.Message = new DialogOption(message, ev);
        dialog.Options.Clear();
        dialog.InputEvent = null;
        dialog.ConfirmEvent = ev;
        Dirty(actor, dialog);

        _ui.TryOpenUi(actor, DialogUiKey.Key, actor);
    }

    public void OpenConfirmation(EntityUid actor, string title, string message, object ev)
    {
        OpenConfirmation(actor, actor, title, message, ev);
    }

    private DialogComponent PrepareDialog(EntityUid target, EntityUid actor)
    {
        var dialog = EnsureComp<DialogComponent>(actor);
        dialog.EventTarget = target;
        return dialog;
    }

    private bool CanSubmitDialog(
        Entity<DialogComponent> dialog,
        EntityUid actor,
        BoundUserInterfaceMessage message)
    {
        if (actor != dialog.Owner || !Exists(dialog.Comp.EventTarget))
            return false;

        var attempt = new BoundUserInterfaceMessageAttempt(
            actor,
            dialog.Comp.EventTarget,
            DialogUiKey.Key,
            message);
        RaiseLocalEvent(attempt);
        if (attempt.Cancelled)
            return false;

        RaiseLocalEvent(dialog.Comp.EventTarget, attempt);
        return !attempt.Cancelled;
    }

    public int CalculateEffectiveLength(ReadOnlySpan<char> text, bool smartCheck = false)
    {
        if (!smartCheck)
        {
            return text.Length;
        }

        var length = 0;
        var previousSpace = false;

        foreach (var ch in text)
        {
            var isSpace = ch == ' ';

            if (isSpace && previousSpace)
                continue;

            length++;
            previousSpace = isSpace;
        }

        return length;
    }

    public string TrimToLimit(ReadOnlySpan<char> text, int maxLength, bool smartCheck = false)
    {
        if (maxLength <= 0)
            return string.Empty;

        if (!smartCheck)
        {
            if (text.Length <= maxLength)
                return text.ToString();

            return text[..maxLength].ToString();
        }

        var builder = new StringBuilder(text.Length);
        var length = 0;
        var consecutiveSpaces = 0;
        var previousSpace = false;

        foreach (var ch in text)
        {
            var isSpace = ch == ' ';

            if (isSpace)
            {
                if (previousSpace)
                {
                    consecutiveSpaces++;
                    // Skip 4th and subsequent consecutive spaces
                    if (consecutiveSpaces >= 3)
                        continue;
                }
                else
                {
                    consecutiveSpaces = 1;
                }
            }
            else
            {
                consecutiveSpaces = 0;
            }

            var countsTowardsLimit = !(isSpace && previousSpace);

            if (countsTowardsLimit && length >= maxLength)
                break;

            if (countsTowardsLimit)
                length++;

            builder.Append(ch);
            previousSpace = isSpace;
        }

        return builder.ToString();
    }
}
