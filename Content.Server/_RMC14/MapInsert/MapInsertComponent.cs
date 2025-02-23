using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.MapInsert;

[RegisterComponent, EntityCategory("Spawner")]
[Access(typeof(MapInsertSystem))]
public sealed partial class MapInsertComponent : Component
{
    [DataField(required: true)]
    public ResPath? Spawn;

    [DataField]
    public Vector2 Offset;

    [DataField]
    public bool ClearEntities;

    [DataField]
    public bool ClearDecals;

    [DataField]
    public bool ReplaceAreas;

    [DataField]
    public float Probability = 1.0f;

}
