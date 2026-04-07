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
        _ui.CloseUi(ent.Owner, DialogUiKey.Key);

        var index = args.Index;
        if (index < 0 || !ent.Comp.Options.TryGetValue(index, out var option))
            return;

        var ev = new DialogChosenEvent(args.Actor, index);
        RaiseLocalEvent(ent, ref ev);

        if (option.Event != null)
            RaiseLocalEvent(ent, ref option.Event, true);
    }

    private void OnDialogInput(Entity<DialogComponent> ent, ref DialogInputBuiMsg args)
    {
        _ui.CloseUi(ent.Owner, DialogUiKey.Key);

        if (ent.Comp.InputEvent == null)
            return;

        var msg = TrimToLimit(args.Input, ent.Comp.CharacterLimit, ent.Comp.SmartCheck);

        ent.Comp.InputEvent = ent.Comp.InputEvent with { Message = msg };
        RaiseLocalEvent(ent, (object) ent.Comp.InputEvent);
    }

    private void OnDialogConfirm(Entity<DialogComponent> ent, ref DialogConfirmBuiMsg args)
    {
        _ui.CloseUi(ent.Owner, DialogUiKey.Key);

        if (ent.Comp.ConfirmEvent != null)
            RaiseLocalEvent(ent, ent.Comp.ConfirmEvent);
    }

    private void OnDialogClosed(Entity<DialogComponent> ent, ref BoundUIClosedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        RemComp<DialogComponent>(ent);
    }

    public void OpenOptions(EntityUid target, EntityUid actor, string title, List<DialogOption> options, string message = "")
    {
        var dialog = EnsureComp<DialogComponent>(target);
        dialog.Title = title;
        dialog.Message = new DialogOption(message);
        dialog.DialogType = DialogType.Options;
        dialog.Options = options;
        Dirty(target, dialog);

        _ui.TryOpenUi(target, DialogUiKey.Key, actor);
    }

    public void OpenOptions(EntityUid actor, string title, List<DialogOption> options, string message = "")
    {
        OpenOptions(actor, actor, title, options, message);
    }

    public void OpenInput(EntityUid target, EntityUid actor, string message, DialogInputEvent? ev, bool largeInput = false, int characterLimit = 200, int minCharacterLimit = 0, bool smartCheck = false, bool autoFocus = true)
    {
        var dialog = EnsureComp<DialogComponent>(target);
        dialog.DialogType = DialogType.Input;
        dialog.Message = new DialogOption(message, ev);
        dialog.InputEvent = ev;
        dialog.LargeInput = largeInput;
        dialog.CharacterLimit = characterLimit;
        dialog.MinCharacterLimit = minCharacterLimit;
        dialog.SmartCheck = smartCheck;
        dialog.AutoFocus = autoFocus;

        Dirty(target, dialog);

        _ui.TryOpenUi(target, DialogUiKey.Key, actor);
    }

    public void OpenInput(EntityUid actor, string message, DialogInputEvent? ev, bool largeInput = false, int characterLimit = 200, int minCharacterLimit = 0, bool smartCheck = false, bool autoFocus = true)
    {
        OpenInput(actor, actor, message, ev, largeInput, characterLimit, minCharacterLimit, smartCheck, autoFocus);
    }

    public void OpenConfirmation(EntityUid target, EntityUid actor, string title, string message, object ev)
    {
        var dialog = EnsureComp<DialogComponent>(target);
        dialog.DialogType = DialogType.Confirm;
        dialog.Title = title;
        dialog.Message = new DialogOption(message, ev);
        dialog.ConfirmEvent = ev;
        Dirty(target, dialog);

        _ui.TryOpenUi(target, DialogUiKey.Key, actor);
    }

    public void OpenConfirmation(EntityUid actor, string title, string message, object ev)
    {
        OpenConfirmation(actor, actor, title, message, ev);
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
