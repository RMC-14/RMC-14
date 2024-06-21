using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Medical.HUD.Components;

/// <summary>
/// The holocard state used to indicate which holocard description and icon to show
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HolocardStateComponent : Component
{
    [DataField, AutoNetworkedField]
    public HolocardStatus HolocardStatus = HolocardStatus.None;
}
