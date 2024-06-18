using Content.Shared.Administration;
using Content.Shared.Administration.Managers;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Shared._CM14.Admin;

public abstract class SharedCMAdminSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminManager _admin = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<CMAdminVerb>>(OnXenoGetVerbs);
    }

    private void OnXenoGetVerbs(GetVerbsEvent<CMAdminVerb> args)
    {
        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;
        if (!CanUse(player))
            return;

        args.Verbs.Add(new CMAdminVerb
        {
            Text = Loc.GetString("cm-ui-open-cm-actions"),
            Act = () =>
            {
                OpenBui(player, args.Target);
            }
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
