using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.TacticalMap;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class TacticalMapLayerData
{
   [DataField]
   public Dictionary<int, TacticalMapBlip> Blips = new();

   [DataField]
   public Dictionary<int, TacticalMapBlip> LastUpdateBlips = new();

   [DataField]
   public List<TacticalMapLine> Lines = new();

   [DataField]
   public Dictionary<Vector2i, string> Labels = new();
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
[Access(typeof(SharedTacticalMapSystem))]
public sealed partial class TacticalMapComponent : Component
{
   [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
   public TimeSpan NextUpdate = TimeSpan.FromSeconds(1);

   [DataField]
   public string MapId = string.Empty;

   [DataField]
   public string DisplayName = string.Empty;

   [DataField]
   public Dictionary<ProtoId<TacticalMapLayerPrototype>, TacticalMapLayerData> Layers = new();

   [DataField]
   public bool MapDirty;
}
