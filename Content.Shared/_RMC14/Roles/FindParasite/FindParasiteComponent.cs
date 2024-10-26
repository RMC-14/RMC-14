using Robust.Shared.GameStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Roles.FindParasite;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FindParasiteComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<string, NetEntity> ActiveParasiteSpawners = new();
}
