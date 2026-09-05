using Content.Shared.Explosion;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared._RMC14.Explosion;

/// <summary>
/// Modifies explosion resistance while the entity's physics body is not collidable.
/// </summary>
[RegisterComponent]
[Access(typeof(SharedRMCExplosionSystem))]
public sealed partial class RMCExplosionResistanceWhenNotCollidableComponent : Component
{
    [DataField(required: true,
        customTypeSerializer: typeof(PrototypeIdDictionarySerializer<float, ExplosionPrototype>))]
    public Dictionary<string, float> Modifiers = new();
}
