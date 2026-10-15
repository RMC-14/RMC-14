using Content.Server.Chat.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Tutorial;

public sealed partial class RMCTutorialNPCSystem : EntitySystem
{
    [Dependency] private readonly GrammarSystem _grammarSystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCTutorialNPCComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RMCTutorialNPCComponent, StartCollideEvent>(OnCollide);
    }

    private void OnStartup(Entity<RMCTutorialNPCComponent> ent, ref ComponentStartup args)
    {
        // Can't set gender via component as it gets overwritten on spawn.
        if (TryComp<GrammarComponent>(ent.Owner, out var grammar))
        {
            _grammarSystem.SetGender((ent.Owner, grammar), Gender.Epicene);
        }
    }

    private void OnCollide(EntityUid uid, RMCTutorialNPCComponent component, ref StartCollideEvent args)
    {
        // Ensures triggering entity is a member of a given faction.
        var subject = args.OtherEntity;
        if (!TryComp<RMCTutorialDummyComponent>(subject, out var tutComp) || !TryComp<NpcFactionMemberComponent>(subject, out var factionComp))
            return;
        if (!EntityManager.System<NpcFactionSystem>().IsMemberOfAny(uid, tutComp.Factions))
            return;

        // If it hasn't yet started, set initial Index
        if (component.LineIndex < 0)
            component.LineIndex = 0;

        if (!component.AutoLine)
            SayNextVoiceline(uid, component);
    }

    public override void Update(float frameTime)
    {
        var tutorialNPCQuery = EntityQueryEnumerator<RMCTutorialNPCComponent>();

        while (tutorialNPCQuery.MoveNext(out var uid, out var component))
        {
            if (component.AutoLine)
                SayNextVoiceline(uid, component);
        }
        base.Update(frameTime);
    }

    private void SayNextVoiceline(EntityUid uid, RMCTutorialNPCComponent component)
    {
        // Prevent line if index is out of range
        if ((component.LineIndex >= component.Voicelines.Count) || (component.LineIndex < 0))
            return;

        // Limits triggering to the delay times set in component
        if (_timing.CurTime < (component.TimeSinceLastLine + TimeSpan.FromSeconds(component.LineDelay)) ||
            _timing.CurTime < (component.TimeSinceEnd +  TimeSpan.FromSeconds(component.ResetDelay)) )
            return;

        // Send next voiceline and update state.
        _chat.TrySendInGameICMessage(uid, component.Voicelines[component.LineIndex], InGameICChatType.Speak, ChatTransmitRange.Normal);
        component.LineIndex += 1;
        component.TimeSinceLastLine = _timing.CurTime;

        // If at end of the dialogue, note the end time
        if (component.LineIndex == component.Voicelines.Count)
        {
            component.TimeSinceEnd = _timing.CurTime;
            Log.Info($"{component.ResetDelay}, {component.LineIndex}");
            // Reset index to start if looping is enabled, else leave out of index to prevent looping
            if (component.ResetDelay > 0)
                component.LineIndex = -1;
        }
    }
}
