using Content.Shared.Damage;
using Content.Shared.Tools;
using Robust.Shared.Prototypes;

namespace Content.Server.Damage.Components;

[RegisterComponent]
public sealed partial class DamageOnToolInteractComponent : Component
{
    [DataField]
    public ProtoId<ToolQualityPrototype> Tools { get; private set; }

    // TODO: Remove this snowflake stuff, make damage per-tool quality perhaps?
    [DataField]
    public DamageSpecifier? WeldingDamage { get; private set; }

    /// <summary>
    /// Chance that an activated welder applies <see cref="WeldingDamage"/>.
    /// </summary>
    [DataField]
    public float WeldingDamageChance { get; private set; } = 1f;

    [DataField]
    public DamageSpecifier? DefaultDamage { get; private set; }
}
