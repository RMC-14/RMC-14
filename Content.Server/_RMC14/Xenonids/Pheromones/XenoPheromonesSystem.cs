using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Mind;
using Content.Shared.Mind;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Pheromones;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Actions;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Collections;
using Robust.Shared.Network;
using Robust.Shared.Threading;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Xenonids.Pheromones;

public sealed class XenoPheromonesSystem : SharedXenoPheromonesSystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly MindSystem _mind = default!;
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
