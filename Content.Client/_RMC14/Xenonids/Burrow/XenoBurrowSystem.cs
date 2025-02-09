using Content.Client.Players;
using Content.Shared._RMC14.NightVision;
using Content.Shared._RMC14.Stealth;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Burrow;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._RMC14.Xenonids.Burrow;

public sealed partial class XenoBurrowSystem : SharedXenoBurrowSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoBurrowComponent, AfterAutoHandleStateEvent>(OnBurrowChange);
    }

    private void OnBurrowChange(EntityUid ent, XenoBurrowComponent comp, ref AfterAutoHandleStateEvent args)
    {
        var localEntity = _player.LocalEntity;
        var isXeno = HasComp<XenoComponent>(localEntity);
        if (TryComp(ent, out SpriteComponent? spriteComp))
        {
            spriteComp.Visible = !comp.Active || isXeno;
        }

        if (TryComp(ent, out RMCNightVisionVisibleComponent? nightVisionVisibleComp))
        {
            if (comp.Active && !isXeno)
            {
                nightVisionVisibleComp.Transparency = 1f;
            }
            else
            {
                nightVisionVisibleComp.Transparency = null;
            }
        }

    }
}
