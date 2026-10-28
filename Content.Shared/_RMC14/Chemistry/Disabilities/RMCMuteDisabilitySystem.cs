using Content.Shared.Speech;

namespace Content.Shared._RMC14.Chemistry.Disabilities;

public sealed class RMCMuteDisabilitySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCMuteDisabilityComponent, SpeakAttemptEvent>(OnSpeakAttempt);
    }

    private void OnSpeakAttempt(Entity<RMCMuteDisabilityComponent> ent, ref SpeakAttemptEvent args)
    {
        args.Cancel();
    }
}
