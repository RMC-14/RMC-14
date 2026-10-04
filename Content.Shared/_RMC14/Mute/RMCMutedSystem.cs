using Content.Shared.Popups;
using Content.Shared.Speech;

namespace Content.Shared._RMC14.Mute;

public sealed class RMCMutedSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCMutedComponent, SpeakAttemptEvent>(OnSpeakAttempt);
    }

    private void OnSpeakAttempt(Entity<RMCMutedComponent> ent, ref SpeakAttemptEvent args)
    {
        _popup.PopupEntity(Loc.GetString("speech-muted"), ent, ent);
        args.Cancel();
    }
}
