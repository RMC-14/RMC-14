using Content.Shared.Damage.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Melee.Components;

/// <summary>
/// Plays the specified sound upon receiving damage of the specified type.
/// </summary>
[RegisterComponent]
public sealed partial class MeleeSoundComponent : Component
{
    /// <summary>
    /// Specified sounds to apply when the entity takes damage with the specified group.
    /// Will fallback to defaults if none specified.
    /// </summary>
    [DataField("soundGroups")]
    public Dictionary<ProtoId<DamageGroupPrototype>, SoundSpecifier>? SoundGroups;

    /// <summary>
    /// Specified sounds to apply when the entity takes damage with the specified type.
    /// Will fallback to defaults if none specified.
    /// </summary>
    [DataField("soundTypes")]
    public Dictionary<ProtoId<DamageTypePrototype>, SoundSpecifier>? SoundTypes;

    /// <summary>
    /// Sound that plays if no damage is done.
    /// </summary>
    [DataField("noDamageSound")] public SoundSpecifier? NoDamageSound;
}
