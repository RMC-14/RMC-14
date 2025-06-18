using Content.Shared._RMC14.Input;
using Robust.Shared.Input.Binding;

namespace Content.Shared._RMC14.Xenonids.Rest;

public sealed class XenoRestBindInputSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(CMKeyFunctions.RMCRest,
                InputCmdHandler.FromDelegate(session =>
                    {
                        var ent = session?.AttachedEntity;
                        if (ent != null && HasComp<XenoComponent>(ent.Value))
                        {
                            var ev = new XenoRestActionEvent();
                            RaiseLocalEvent(ent.Value, ref ev);
                        }
                    },
                    handle: false
                ))
            .Register<XenoRestBindInputSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<XenoRestBindInputSystem>();
    }
}
