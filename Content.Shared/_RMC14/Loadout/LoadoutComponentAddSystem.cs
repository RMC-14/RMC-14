using Content.Shared._RMC14.Admin;
using Content.Shared._RMC14.Synth;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Loadout;

public sealed class LoadoutComponentAddSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LoadoutComponentAddEvent>(OnComponentAdd);
    }

    private void OnComponentAdd(ref LoadoutComponentAddEvent ev)
    {
        if (!ev.Loadout.ComponentsAdd)
            return;

        var e = ev.Entity;

        if (HasComp<RMCAdminSpawnedComponent>(e))
            return;

        foreach (var entProtoId in ev.Loadout.Equipment.Values)
        {
            if (!_prototype.TryIndex(entProtoId, out var proto))
            {
                Log.Warning("attempting to index Entity prototype failed");
                return;
            }

            EntityManager.AddComponents(e,  proto);
        }
    }
}
