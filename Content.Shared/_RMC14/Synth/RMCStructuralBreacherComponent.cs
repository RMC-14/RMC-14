using Content.Shared.Damage;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Synth;

/// <summary>
/// Adds extra damage when a melee weapon is used against approved structural targets.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RMCStructuralBreacherComponent : Component
{
    /// <summary>
    /// Extra damage applied after a valid melee hit.
    /// </summary>
    [DataField]
    public DamageSpecifier BonusDamage = new()
    {
        DamageDict =
        {
            ["Structural"] = 120,
            ["Blunt"] = 60,
        },
    };

    /// <summary>
    /// If true, only synths can apply the extra breaching damage.
    /// </summary>
    [DataField]
    public bool RequiresSynth = true;

    /// <summary>
    /// Targets that can receive the extra breaching damage.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist = new()
    {
        Tags = new()
        {
            "Wall",
        },
        Components = new[]
        {
            "Door",
            "ResinDoor",
        },
    };
}
