using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Medical.Scanner;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HealthScannerComponent : Component
{
    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sound;
}
