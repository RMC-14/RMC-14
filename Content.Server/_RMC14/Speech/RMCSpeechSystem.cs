using Content.Server.Speech.Components;

namespace Content.Server._RMC14.Speech;

public sealed class RMCSpeechSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        var commandAccents = EntityQueryEnumerator<CommandAccentComponent>();
        while (commandAccents.MoveNext(out var uid, out _))
        {
            RemCompDeferred<CommandAccentComponent>(uid);
            RemCompDeferred<StutteringAccentComponent>(uid);
            RemCompDeferred<FrontalLispComponent>(uid);
        }
    }
}
