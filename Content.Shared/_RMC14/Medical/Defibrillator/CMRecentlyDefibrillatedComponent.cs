using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Defibrillator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(CMDefibrillatorSystem))]
public sealed partial class CMRecentlyDefibrillatedComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan RemoveAfter = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan RemoveAt;
}
