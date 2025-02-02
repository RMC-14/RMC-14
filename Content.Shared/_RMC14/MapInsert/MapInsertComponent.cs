using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.MapInsert;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedMapInsertSystem))]
public sealed partial class MapInsertComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ResPath? Spawn;

    [DataField, AutoNetworkedField]
    public Vector2 Offset;

    [DataField, AutoNetworkedField]
    public bool ClearEntities;

    [DataField, AutoNetworkedField]
    public bool ReplaceAreas;

}
