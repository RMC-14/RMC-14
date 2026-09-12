using Content.Shared._RMC14.Weapons.Ranged;
using Robust.Shared.Input.Binding;

namespace Content.Shared._RMC14.Input;

public sealed class RMCWeaponKeybindSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(CMKeyFunctions.RMCUnloadWeapon,
                InputCmdHandler.FromDelegate(session =>
                {
                    if (session?.AttachedEntity is { } user)
                        TryUnloadWeapon(user);
                }, handle: false))
            .Register<RMCWeaponKeybindSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<RMCWeaponKeybindSystem>();
    }

    public bool TryUnloadWeapon(EntityUid user)
    {
        var ev = new RMCUnloadWeaponEvent(false);
        RaiseLocalEvent(user, ref ev);
        return ev.Handled;
    }
}
