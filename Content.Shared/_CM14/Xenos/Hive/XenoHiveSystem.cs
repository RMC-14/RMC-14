using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Content.Shared._CM14.Xenos.Hive;

public sealed class XenoHiveSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly INetManager _net = default!;

    public void CreateHive(string name)
    {
        if (_net.IsClient)
            return;

        var ent = Spawn(null, MapCoordinates.Nullspace);
        EnsureComp<HiveComponent>(ent);
        _metaData.SetEntityName(ent, name);
    }
}
