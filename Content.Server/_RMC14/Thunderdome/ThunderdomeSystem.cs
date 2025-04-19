using Content.Server.Radio;
using Content.Shared._RMC14.Thunderdome;

namespace Content.Server._RMC14.Thunderdome;

public sealed class ThunderdomeSystem : EntitySystem
{
    private EntityQuery<ThunderdomeMapComponent> _thunderdomeMapQuery;

    public override void Initialize()
    {
        _thunderdomeMapQuery = GetEntityQuery<ThunderdomeMapComponent>();
        SubscribeLocalEvent<RadioReceiveAttemptEvent>(OnRadioReceiveAttempt);
    }

    private void OnRadioReceiveAttempt(ref RadioReceiveAttemptEvent ev)
    {
        if (ev.Cancelled)
            return;

        var sourceThunderdome = _thunderdomeMapQuery.HasComp(ev.RadioSource);
        var targetThunderdome = _thunderdomeMapQuery.HasComp(ev.RadioReceiver);
        if (sourceThunderdome != targetThunderdome)
            ev.Cancelled = true;
    }
}
