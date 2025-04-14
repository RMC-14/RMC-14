using Content.Shared._RMC14.Rangefinder.Spotting;

namespace Content.Server._RMC14.Weapons.Ranged.Sniper;

public sealed class RMCSpottingSystem : SharedRMCSpottingSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RequestSpotEvent>(OnSpotRequest);
    }

    private void OnSpotRequest(RequestSpotEvent ev, EntitySessionEventArgs args)
    {
        SpotRequested(ev.SpottingTool, ev.User, ev.Target);
    }
}
