using Content.Shared._RMC14.Medical.Scanner;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.BodyScanner;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedBodyScannerSystem))]
public sealed partial class BodyScannerConsoleComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? LinkedBodyScanner;

    [DataField, AutoNetworkedField]
    public HealthScanDetailLevel DetailLevel = HealthScanDetailLevel.BodyScan;

    [DataField]
    public SoundSpecifier ScanSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/screen_output1.ogg");
}
