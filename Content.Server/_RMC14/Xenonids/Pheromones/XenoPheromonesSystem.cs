using System.Linq;
using Content.Server.Chat.Managers;
using Content.Shared._RMC14.Xenonids.Pheromones;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Xenonids.Pheromones;

public sealed class XenoPheromonesSystem : SharedXenoPheromonesSystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly ActorSystem _actors = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private const string HelpButtonText = "rmc-xeno-pheromones-help";

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<XenoPheromonesComponent>(XenoPheromonesUI.Key, subs =>
        {
            subs.Event<XenoPheromonesHelpButtonBuiMsg>(OnXenoPheromonesHelpButton);
        });
    }

    private void OnXenoPheromonesHelpButton(Entity<XenoPheromonesComponent> xeno, ref XenoPheromonesHelpButtonBuiMsg args)
    {
        var msg = Loc.GetString(HelpButtonText);
        var session = _actors.GetSession(xeno.Owner);

        if (session != null)
            _chat.DispatchServerMessage(session, msg);

        _ui.CloseUi(xeno.Owner, XenoPheromonesUI.Key, xeno);
    }
}
