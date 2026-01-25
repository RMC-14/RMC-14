using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
[Access(typeof(SharedTacticalMapSystem))]
public sealed partial class TacticalMapComponent : Component
{
   [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
   public TimeSpan NextUpdate = TimeSpan.FromSeconds(1);

   [DataField]
   public Dictionary<int, TacticalMapBlip> MarineBlips = new();

   [DataField]
   public Dictionary<int, TacticalMapBlip> LastUpdateMarineBlips = new();

   [DataField]
   public Dictionary<int, TacticalMapBlip> XenoBlips = new();

   [DataField]
   public Dictionary<int, TacticalMapBlip> XenoStructureBlips = new();

   [DataField]
   public Dictionary<int, TacticalMapBlip> LastUpdateXenoBlips = new();

   [DataField]
   public Dictionary<int, TacticalMapBlip> LastUpdateXenoStructureBlips = new();

   [DataField]
   public List<TacticalMapLine> MarineLines = new();

   [DataField]
   public List<TacticalMapLine> XenoLines = new();

   [DataField]
   public Dictionary<Vector2i, string> MarineLabels = new();

   [DataField]
   public Dictionary<Vector2i, string> XenoLabels = new();

   [DataField]
   public bool MapDirty;
}
