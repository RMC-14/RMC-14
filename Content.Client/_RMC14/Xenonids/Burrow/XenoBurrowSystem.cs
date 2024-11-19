using Content.Client.Players;
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

        SubscribeLocalEvent<XenoBurrowComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<XenoBurrowComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<XenoBurrowComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        sprite.PostShader = _prototype.Index<ShaderPrototype>("RMCInvisible").InstanceUnique();
    }

    private void OnShutdown(Entity<XenoBurrowComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        sprite.PostShader = null;
    }

    public override void Update(float frameTime)
    {
        var burrowed = EntityQueryEnumerator<XenoBurrowComponent, SpriteComponent>();
        var localEntity = _player.LocalEntity;
        var isXeno = HasComp<XenoComponent>(localEntity);

        while (burrowed.MoveNext(out var uid, out var comp, out var sprite))
        {
            var opacity = 0f;

            if (isXeno)
            {
                opacity = XenoBurrowComponent.BurrowOpacity;
            }
            if (!comp.Active)
            {
                opacity = 1f;
            }
            sprite.PostShader?.SetParameter("visibility", opacity);
        }
    }
}
