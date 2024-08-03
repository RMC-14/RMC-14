using Content.Shared._RMC14.EyeProtection;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._RMC14.EyeProtection;

public sealed class RMCEyeProtectionSystem : RMCSharedEyeProtectionSystem
{
    //[Dependency] private readonly ILightManager _light = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCEyeProtectionComponent, LocalPlayerAttachedEvent>(OnEyeProtectionAttached);
        SubscribeLocalEvent<RMCEyeProtectionComponent, LocalPlayerDetachedEvent>(OnEyeProtectionDetached);
    }

    private void OnEyeProtectionAttached(Entity<RMCEyeProtectionComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        EyeProtectionChanged(ent);
    }

    private void OnEyeProtectionDetached(Entity<RMCEyeProtectionComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        Off();
    }

    protected override void EyeProtectionChanged(Entity<RMCEyeProtectionComponent> ent)
    {
        if (ent != _player.LocalEntity)
            return;

        if (ent.Comp.Enabled)
        {
            On(ent);
        }
        else
        {
            Off();
        }
    }

    protected override void EyeProtectionRemoved(Entity<RMCEyeProtectionComponent> ent)
    {
        if (ent != _player.LocalEntity)
            return;

        Off();
    }

    private void Off()
    {
        _overlay.RemoveOverlay(new EyeProtectionOverlay());
        //_light.DrawLighting = true;
    }

    private void On(Entity<RMCEyeProtectionComponent> ent)
    {
        if (ent.Comp.Overlay)
            _overlay.AddOverlay(new EyeProtectionOverlay());

        //_light.DrawLighting = false;
    }
}
