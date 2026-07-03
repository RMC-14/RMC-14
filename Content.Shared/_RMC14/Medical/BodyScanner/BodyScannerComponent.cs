using System.Numerics;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.BodyScanner;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedBodyScannerSystem))]
public sealed partial class BodyScannerComponent : Component
{
    [DataField]
    public string ContainerId = "body_scanner";

    [DataField, AutoNetworkedField]
    public EntityUid? Occupant;

    /// <summary>
    /// The prototype to spawn the console. If null, no console is spawned.
    /// </summary>
    [DataField]
    public EntProtoId<BodyScannerConsoleComponent>? SpawnConsolePrototype = "RMCBodyScannerConsole";

    /// <summary>
    /// Offset for spawning the console relative to the body scanner.
    /// This is applied based on the body scanner's rotation.
    /// </summary>
    [DataField]
    public Vector2 ConsoleSpawnOffset = new(1, 0);

    [DataField, AutoNetworkedField]
    public TimeSpan ExitStun = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public EntityUid? LinkedConsole;

    [DataField]
    public SoundSpecifier EjectSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/hydraulics_3.ogg");

    [DataField]
    public SoundSpecifier InsertSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/scanning_pod1.ogg");
}

[Serializable, NetSerializable]
public enum BodyScannerVisuals : byte
{
    Occupied
}

[Serializable, NetSerializable]
public enum BodyScannerVisualLayers
{
    Base
}
