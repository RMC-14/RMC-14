
using Content.Shared._CM14.Medical;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Medical.Components;
/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed class HolocardComponent : Component
{
    [DataField, AutoNetworkedField]
    public HolocardStaus HolocardStaus = HolocardStaus.None;
}
