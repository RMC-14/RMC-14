using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared._RMC14.Marines.Skills;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.PowerLoader;

/// <summary>
/// For entities that can be "detached" from another thing via interaction with said entity
/// with an empty hand of the power loader
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PowerLoaderDetachableComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan DetachDelay = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> DetachSkill = "RMCSkillEngineer";
}
