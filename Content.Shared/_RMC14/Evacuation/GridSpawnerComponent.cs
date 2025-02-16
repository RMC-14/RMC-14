using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Evacuation;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedEvacuationSystem))]
public sealed partial class GridSpawnerComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ResPath? Spawn;

    [DataField, AutoNetworkedField]
    public Vector2 Offset;
}
