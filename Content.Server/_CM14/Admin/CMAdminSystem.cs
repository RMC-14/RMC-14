using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Server._CM14.Admin;

public sealed class CMAdminSystem : EntitySystem
{
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly EuiManager _eui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<Verb>>(OnXenoGetVerbs);
    }

    private void OnXenoGetVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;
        if (!CMAdminEui.CanUse(_admin, player))
            return;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("cm-ui-open-cm-actions"),
            Category = VerbCategory.Admin,
            Act = () =>
            {
                OpenBui(player, args.Target);
            }
        });
    }

    private void OpenBui(ICommonSession player, EntityUid target)
    {
        if (!CMAdminEui.CanUse(_admin, player))
            return;

        _eui.OpenEui(new CMAdminEui(target), player);
    }
}
