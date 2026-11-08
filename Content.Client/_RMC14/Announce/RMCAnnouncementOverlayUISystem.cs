using Content.Shared._RMC14.Announce;

namespace Content.Client._RMC14.Announce;

public sealed class RMCAnnouncementOverlayUISystem : EntitySystem
{
    public event Action<RMCAnnouncementOverlayMsg>? AnnouncementReceived;

    public override void Initialize()
    {
        SubscribeNetworkEvent<RMCAnnouncementOverlayMsg>(OnAnnouncementOverlay);
    }

    private void OnAnnouncementOverlay(RMCAnnouncementOverlayMsg msg, EntitySessionEventArgs args)
    {
        AnnouncementReceived?.Invoke(msg);
    }
}
