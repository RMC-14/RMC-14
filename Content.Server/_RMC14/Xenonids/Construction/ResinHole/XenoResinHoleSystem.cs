using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Xenonids.Construction.ResinHole;
using Robust.Server.GameObjects;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._RMC14.Xenonids.Construction.ResinHole;

public sealed partial class XenoResinHoleSystem : SharedXenoResinHoleSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoResinHoleComponent, ViewSubscriberAddedEvent>(OnEnterPVS);
    }

    private void OnEnterPVS(Entity<XenoResinHoleComponent> resinHole, ref ViewSubscriberAddedEvent args)
    {
        if (!HasComp<MarineComponent>(args.Subscriber.AttachedEntity))
        {
            return;
        }
        var ev = new HideResinHoleOutlineMessage(GetNetEntity(resinHole.Owner));
        RaiseNetworkEvent(ev, args.Subscriber);
    }
}
