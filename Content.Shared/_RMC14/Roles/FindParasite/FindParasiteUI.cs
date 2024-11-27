using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Roles.FindParasite;

[Serializable, NetSerializable]
public sealed partial class FollowParasiteSpawnerMessage : BoundUserInterfaceMessage
{
    public NetEntity Spawner;
    public FollowParasiteSpawnerMessage(NetEntity spawner)
    {
        Spawner = spawner;
    }
}

[Serializable, NetSerializable]
public sealed partial class TakeParasiteRoleMessage : BoundUserInterfaceMessage
{
    public NetEntity Spawner;
    public TakeParasiteRoleMessage(NetEntity spawner)
    {
        Spawner = spawner;
    }
}

[Serializable, NetSerializable]
public sealed partial class RefreshActiveParasiteSpawnersMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed partial class FindParasiteUIState : BoundUserInterfaceState
{
    public List<SpawnerData> ActiveParasiteSpawners = new();
}

[Serializable, NetSerializable]
public sealed partial class SpawnerData
{
    public string Name;
    public NetEntity Spawner;

    public SpawnerData(string name, NetEntity spawner)
    {
        Name = name;
        Spawner = spawner;
    }
}

[Serializable, NetSerializable]
public enum XenoFindParasiteUI : byte
{
    Key
}
