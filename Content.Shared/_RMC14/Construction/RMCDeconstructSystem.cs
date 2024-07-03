using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Storage;
using Content.Shared.Verbs;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Construction;

public sealed class RMCDeconstructSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCDeconstructComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetVerbs(Entity<RMCDeconstructComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var user = args.User;

        var v = new Verb
        {
            Priority = 1,
            Text = Loc.GetString(ent.Comp.VerbText ?? "rmc-deconstruct-verb"),
            Impact = LogImpact.Low,
            DoContactInteraction = true,
            Act = () =>
            {
                Deconstruct(ent, user);
            }
        };

        args.Verbs.Add(v);
    }

    private void Deconstruct(Entity<RMCDeconstructComponent> ent, EntityUid user)
    {
        if (ent.Comp.SpawnEntries is not null)
        {
            var coords = Transform(ent).Coordinates;

            EntityUid? entityToPlaceInHands = null;
            foreach (var proto in EntitySpawnCollection.GetSpawns(ent.Comp.SpawnEntries, _random))
            {
                entityToPlaceInHands = SpawnAtPosition(proto, coords);
            }

            if (entityToPlaceInHands != null)
            {
                _hands.PickupOrDrop(user, entityToPlaceInHands.Value);
            }
        }

        if (ent.Comp.DeleteEntity)
            Del(ent);
    }
}
