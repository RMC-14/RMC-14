using Content.Client.Interactable.Components;
using Content.Shared._RMC14.Xenonids.Construction.ResinHole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._RMC14.Xenonids.Construction.ResinHole;

public sealed partial class XenoResinHoleSystem : SharedXenoResinHoleSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<HideResinHoleOutlineMessage>(HideResinHoleOutline);
    }

    private void HideResinHoleOutline(HideResinHoleOutlineMessage msg)
    {
        var resinHole = GetEntity(msg.ResinHole);
        RemCompDeferred<InteractionOutlineComponent> (resinHole);
    }
}
