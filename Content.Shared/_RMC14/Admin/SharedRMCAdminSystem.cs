using Content.Shared.Administration;
using Content.Shared.Administration.Managers;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Admin;

public abstract class SharedRMCAdminSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminManager _admin = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GetVerbsEvent<RMCAdminVerb>>(OnXenoGetVerbs);
    }

    private void OnXenoGetVerbs(GetVerbsEvent<RMCAdminVerb> args)
    {
        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;
        if (!CanUse(player))
            return;

        args.Verbs.Add(new RMCAdminVerb
        {
            Text = Loc.GetString("rmc-ui-open-rmc-actions"),
            Act = () =>
            {
                OpenBui(player, args.Target);
            },
        });
    }

    protected bool CanUse(ICommonSession player)
    {
        return _admin.HasAdminFlag(player, AdminFlags.Debug);
    }

    protected virtual void OpenBui(ICommonSession player, EntityUid target)
    {
    }
}
