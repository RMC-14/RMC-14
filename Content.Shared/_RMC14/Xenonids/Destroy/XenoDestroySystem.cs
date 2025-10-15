using Content.Shared.Coordinates;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.Destroy;
public sealed partial class XenoDestroySystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoDestroyComponent, XenoDestroyActionEvent>(OnXenoDestroyAction);
    }

    private void OnXenoDestroyAction(Entity<XenoDestroyComponent> xeno, ref XenoDestroyActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (_net.IsServer)
        {
            SpawnAtPosition(xeno.Comp.Telegraph, args.Target);
        }
    }
}
