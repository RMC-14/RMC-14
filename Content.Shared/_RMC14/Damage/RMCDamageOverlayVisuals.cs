using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Damage;

[Serializable, NetSerializable]
public enum RMCDamageOverlayVisuals : byte
{
    DamageOverlay,
    AdditionalDamageOverlay,
}
