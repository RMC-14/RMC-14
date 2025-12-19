using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Random.Names;

public sealed class RMCRandomNameSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCRandomNameComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<RMCRandomNameComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient)
            return;

        var meta = MetaData(ent);

        var baseName = Loc.GetString(ent.Comp.BaseName);
        var postFix = Loc.GetString(ent.Comp.PostFix);
        var finalName = $"{baseName} {postFix}{_random.NextFloat(1, ent.Comp.MaxNumber)}";

        _metaData.SetEntityName(ent, finalName, meta);
    }
}
