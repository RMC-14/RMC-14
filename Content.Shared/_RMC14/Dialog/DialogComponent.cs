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

    [DataField, AutoNetworkedField]
    public object? Event;

    [DataField, AutoNetworkedField]
    public DialogInputEvent? InputEvent;

    [DataField, AutoNetworkedField]
    public object? ConfirmEvent;

    [DataField, AutoNetworkedField]
    public int CharacterLimit = 200;
}
