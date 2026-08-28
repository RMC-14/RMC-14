using Content.Server.Chat.Systems;
using Content.Shared._RMC14.Pushup;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Pushup;

public sealed class RMCPushupEmoteSystem : EntitySystem
{
    [Dependency] private readonly SharedRMCPushupSystem _pushup = default!;

    private static readonly ProtoId<EmotePrototype> PushupRoutineEmote = "RMCPushupRoutine";

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCPushupComponent, EmoteEvent>(OnEmote);
    }

    private void OnEmote(Entity<RMCPushupComponent> ent, ref EmoteEvent args)
    {
        if (args.Emote.ID != PushupRoutineEmote)
            return;

        args.Handled = true;
        _pushup.OpenRoutineDialog(ent.Owner);
    }
}
