using Robust.Shared.Map;

namespace Content.Shared._CM14.Xenos.Hive;

public sealed class XenoHiveSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public void CreateHive(string name)
    {
        var ent = Spawn(null, MapCoordinates.Nullspace);
        EnsureComp<HiveComponent>(ent);
        _metaData.SetEntityName(ent, name);
    }

    public void SetHive(Entity<XenoComponent?> xeno, Entity<HiveComponent?> hive)
    {
        if (!Resolve(xeno, ref xeno.Comp) ||
            !Resolve(hive, ref hive.Comp))
        {
            return;
        }

        xeno.Comp.Hive = hive;
        Dirty(xeno, xeno.Comp);
    }
}
