using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Ghost;
public sealed partial class RMCVisibleToGhostsOnlyComponent : Component;

[Serializable, NetSerializable]
public enum RMCGhostVisibleOnlyVisualLayers
{
    Base,
}
