using Content.Shared.Construction;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Verbs;

namespace Content.Shared._RMC14.Construction;

public sealed class RMCDeconstructSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

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
                var ev = new ConstructionInteractionEvent(user);
                RaiseLocalEvent(ent, ev);

                if (ev.Handled)
                    return;

                _popup.PopupClient(
                    Loc.GetString(ent.Comp.VerbFailedText ?? "rmc-deconstruct-verb-failed",
                        ("entityName", ent.Owner)),
                    user);
            },
        };

        args.Verbs.Add(v);
    }
}
