using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Projectile.Spit.Slowing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoSpitSystem), typeof(XenoProjectileSystem))]
public sealed partial class XenoSlowingSpitProjectileComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool SuperSlow = true;

    [DataField, AutoNetworkedField]
    public TimeSpan Slow = TimeSpan.FromSeconds(8);

    [DataField, AutoNetworkedField]
    public TimeSpan Paralyze = TimeSpan.FromSeconds(3.5);

    [DataField, AutoNetworkedField]
    public bool ArmorResistsKnockdown = true;

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> ResistSkill = "RMCSkillEndurance";

    [DataField, AutoNetworkedField]
    public int ResistLevel = 5;
}
