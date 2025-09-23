using Content.Shared._RMC14.Weapons.Ranged.AimedShot;

namespace Content.Server._RMC14.Weapons.Ranged.Sniper;

public sealed class RMCAimedShotSystem : SharedRMCAimedShotSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RequestAimedShotEvent>(OnAimedShotRequest);
    }

    private void OnAimedShotRequest(RequestAimedShotEvent ev, EntitySessionEventArgs args)
    {
        AimedShotRequested(ev.Gun,ev.User,ev.Target);
    }
}
