using Content.Shared.Construction;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
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

        var v = new Verb
        {
            Priority = 1,
            Text = Loc.GetString(ent.Comp.VerbText ?? "rmc-deconstruct-verb"),
            Impact = LogImpact.Low,
            DoContactInteraction = true,
            Act = () =>
            {
                RaiseLocalEvent(ent, new ConstructionInteractionEvent());
            }
        };

        args.Verbs.Add(v);
    }
}
