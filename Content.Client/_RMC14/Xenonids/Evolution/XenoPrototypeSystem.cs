using Content.Shared._RMC14.Xenonids.Evolution;
using Robust.Client.GameObjects;
using Robust.Client.GameStates;
using Robust.Shared.GameStates;

namespace Content.Client._RMC14.Xenonids.Evolution;

public sealed class XenoPrototypeSystem : EntitySystem
{
    [Dependency] private readonly ClientEntityManager _entities = default!;
    [Dependency] private readonly XenoEvolutionSystem _evolution = default!;
    [Dependency] private readonly IClientGameStateManager _gamestateManager = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesOutsidePrediction = true;
    }

    public override void Update(float frameTime)
    {
        var prototypedXenos = EntityQueryEnumerator<XenoPrototypeComponent, MetaDataComponent>();
        while (prototypedXenos.MoveNext(out var uid, out var comp, out var meta))
        {
            if (!comp.TargetPrototypeId.HasValue)
                continue;

            if (!comp.CurrentPrototypeId.HasValue)
            {
                comp.CurrentPrototypeId = meta.EntityPrototype?.ID;
            }

            if (comp.CurrentPrototypeId == comp.TargetPrototypeId)
            {
                continue;
            }

            if (!_evolution.EnsureXenoPrototype(uid, comp.TargetPrototypeId.Value))
            {
                // failed to ensure xeno prototype for some reason. Stop trying to update
                // the prototype until we get another update from the server.
                comp.TargetPrototypeId = null;
            }
        }
    }
}
