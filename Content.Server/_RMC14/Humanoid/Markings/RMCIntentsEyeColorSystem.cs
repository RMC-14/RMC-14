using Content.Server.Humanoid;
using Content.Server.Humanoid.Systems;
using Content.Shared._RMC14.Humanoid.Markings;
using Content.Shared.CombatMode;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Humanoid.Markings;

public sealed class RMCIntentsEyeColorSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCIntentsEyeColorComponent, MapInitEvent>(OnMapInit,
            after: new[] { typeof(RandomHumanoidAppearanceSystem) });

        SubscribeLocalEvent<RMCIntentsEyeColorComponent, ToggleCombatActionEvent>(OnCombatModeChanged,
            after: new[] { typeof(SharedCombatModeSystem) }
        );

        SubscribeLocalEvent<RMCIntentsEyeColorComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMapInit(Entity<RMCIntentsEyeColorComponent> ent, ref MapInitEvent args)
    {
        var color = GetCombatModeColor(ent);
        SetEyeColor(ent.Owner, color);
    }

    private void OnCombatModeChanged(Entity<RMCIntentsEyeColorComponent> ent, ref ToggleCombatActionEvent args)
    {
        if (_mobState.IsDead(ent.Owner))
            return;

        var color = GetCombatModeColor(ent);
        SetEyeColor(ent.Owner, color);
    }

    private void OnMobStateChanged(Entity<RMCIntentsEyeColorComponent> ent, ref MobStateChangedEvent args)
    {
        if (_mobState.IsDead(ent.Owner))
        {
            SetEyeColor(ent.Owner, ent.Comp.DeadEyeColor);
        }
        else
        {
            var color = GetCombatModeColor(ent);
            SetEyeColor(ent.Owner, color);
        }
    }

    private Color GetCombatModeColor(Entity<RMCIntentsEyeColorComponent> ent)
    {
        var modeColor = _combatMode.IsInCombatMode(ent) switch
        {
            true => ent.Comp.EyeColorHarm,
            false => ent.Comp.EyeColorHelp,
        };

        return modeColor;
    }

    public void SetEyeColor(EntityUid uid, Color color)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
            return;

        humanoid.EyeColor = color;
        Dirty(uid, humanoid);
    }
}
