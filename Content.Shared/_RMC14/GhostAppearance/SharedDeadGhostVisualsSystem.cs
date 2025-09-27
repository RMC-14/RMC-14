using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.CCVar;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Players;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager;

namespace Content.Shared._RMC14.GhostAppearance;

public abstract class SharedDeadGhostVisualsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ISerializationManager _serMan = default!;
    [Dependency] private readonly INetConfigurationManager _netConfigManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCGhostAppearanceComponent, PlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnPlayerAttached(Entity<RMCGhostAppearanceComponent> ent, ref PlayerAttachedEvent args)
    {
        var session = args.Player;

        if (!_netConfigManager.GetClientCVar(session.Channel, RMCCVars.RMCGhostAppearanceFromDeadCharacter))
            return;

        if (session.GetMind() is not { } mindId || !TryComp<MindComponent>(mindId, out var mind))
            return;

        var ownedEntity = mind.OwnedEntity;
        var originalOwnedEntity = GetEntity(mind.OriginalOwnedEntity);

        if (HasComp<GhostComponent>(ownedEntity)) // incase they ghosted
            ownedEntity = originalOwnedEntity;

        ent.Comp.SourceEntity = GetNetEntity(ownedEntity);
        Dirty(ent);

        if (ownedEntity == null)
            return;

        _appearance.CopyData(ownedEntity.Value, ent.Owner);
    }

    protected bool CopyComp<T>(Entity<RMCGhostAppearanceComponent> ent) where T: Component, new()
    {
        if (!GetSrcComp<T>(ent.Comp, out var src))
            return true;

        // remove then re-add to prevent a funny
        RemComp<T>(ent);
        var dest = AddComp<T>(ent);
        _serMan.CopyTo(src, ref dest, notNullableOverride: true);
        Dirty(ent, dest);
        return false;
    }

    private bool GetSrcComp<T>(RMCGhostAppearanceComponent comp, [NotNullWhen(true)] out T? src) where T : Component, new()
    {
        if (TryComp(GetEntity(comp.SourceEntity), out src))
            return true;

        return false;
    }
}
