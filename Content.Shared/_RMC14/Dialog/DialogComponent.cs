using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dialog;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(DialogSystem))]
public sealed partial class DialogComponent : Component
{
    [DataField, AutoNetworkedField]
    public DialogType DialogType;

    [DataField, AutoNetworkedField]
    public string Title;

    [DataField, AutoNetworkedField]
    public DialogOption Message = new(string.Empty);

    [DataField, AutoNetworkedField]
    public List<DialogOption> Options = new();

    // Server-side callback payload. The client only needs the rendered dialog data.
    [DataField]
    public object? Event;

    // Input callbacks stay server-side and are raised when the user submits the dialog.
    [DataField]
    public DialogInputEvent? InputEvent;

    [DataField, AutoNetworkedField]
    public bool LargeInput;

    // Confirm callbacks stay server-side for the same reason.
    [DataField]
    public object? ConfirmEvent;

    [DataField, AutoNetworkedField]
    public int CharacterLimit = 200;

    [DataField, AutoNetworkedField]
    public int MinCharacterLimit;

    [DataField]
    public bool AutoFocus = true;

    [DataField, AutoNetworkedField]
    public bool SmartCheck = false;
}
