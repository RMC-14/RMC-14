using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.CameraShake;
using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Weapons.Common;
using Content.Shared.CombatMode;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Weapons.Ranged;

public sealed class RMCAirShotSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly RMCCameraShakeSystem _cameraShake = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCAirShotComponent, UniqueActionEvent>(OnUniqueAction, before: new[] { typeof(CMGunSystem) } );
        SubscribeLocalEvent<RMCAirShotComponent, AirShotDoAfterEvent>(OnAirShotDoAfter);
    }

    private void OnUniqueAction(Entity<RMCAirShotComponent> ent, ref UniqueActionEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.RequiresCombat && !_combat.IsInCombatMode(args.UserUid))
            return;

        if (ent.Comp.RequiredSkills != null && !_skills.HasAllSkills(args.UserUid, ent.Comp.RequiredSkills))
            return;

        AttemptAirShot(ent, args.UserUid);

        args.Handled = true;
    }

    /// <summary>
    ///     Try to shoot the gun in the air, spawns an entity on the shooters location if it's ammo has the <see cref="RMCAirProjectileComponent"/>.
    /// </summary>
    /// <param name="ent">The entity that wants to shoot into the air</param>
    /// <param name="shooter">The entity using the weapon.</param>
    private void AttemptAirShot(Entity<RMCAirShotComponent> ent, EntityUid shooter)
    {
        var ammoCountEv = new GetAmmoCountEvent();
        RaiseLocalEvent(ent, ref ammoCountEv);

        if (ammoCountEv.Count <= 0)
            return;

        var shooterCoordinates = _transform.GetMoverCoordinates(shooter);

        if (!ent.Comp.IgnoreRoof && !_area.CanCAS(shooterCoordinates))
        {
            var msg = Loc.GetString("rmc-gun-shoot-air-blocked");
            _popup.PopupClient(msg, shooterCoordinates, shooter, PopupType.SmallCaution);
            return;
        }

        var ev = new AirShotDoAfterEvent(GetNetCoordinates(shooterCoordinates));
        var doAfter = new DoAfterArgs(EntityManager, shooter, ent.Comp.PreparationTime, ev, ent)
        {
            BreakOnMove = true,
            NeedHand = true,
            BreakOnHandChange = true,
            MovementThreshold = 0.5f,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnAirShotDoAfter(Entity<RMCAirShotComponent> ent, ref AirShotDoAfterEvent args)
    {
        if (args.DoAfter.Cancelled)
            return;

        if (!TryComp(ent, out GunComponent? gun))
            return;

        var coordinates = GetCoordinates(args.Coordinates);

        var ev = new TakeAmmoEvent(1, new List<(EntityUid? Entity, IShootable Shootable)>(), coordinates, args.User);
        RaiseLocalEvent(ent, ev);

        foreach (var (casing, _) in ev.Ammo)
        {
            if (TryComp(casing, out RMCAirProjectileComponent? projectile))
            {
                if (_net.IsServer)
                    Spawn(projectile.Prototype, GetCoordinates(args.Coordinates));
            }

            // Signal flares don't like being instantly deleted.
            if (HasComp<FlareSignalComponent>(casing))
                QueueDel(casing);
            // Projectiles hurt the shooting entity if they are not deleted on the same tick.
            else
            {
                Del(casing);
            }
        }

        _audio.PlayPredicted(gun.SoundGunshotModified, ent, args.User);

        if (ent.Comp.ShakeAmount > 0)
        {
            var players = Filter.Pvs(args.User, 0.35f);
            var toRemove = new List<ICommonSession>();

            var selfMessage = Loc.GetString("rmc-gun-shoot-air-self", ("weapon", ent));
            _popup.PopupClient(selfMessage, args.User, args.User, PopupType.LargeCaution);

            foreach (var player in players.Recipients)
            {
                if (player.AttachedEntity is not { } playerEnt)
                    continue;

                if (_player.TryGetSessionByEntity(args.User, out var session) && session == player)
                    continue;

                var othersMessage = Loc.GetString("rmc-gun-shoot-air-other", ("user", Identity.Name(args.User, EntityManager, playerEnt)), ("weapon", Identity.Name(ent, EntityManager, playerEnt)));
                _popup.PopupEntity(othersMessage, args.User, player, PopupType.LargeCaution);

                if (player.AttachedEntity != null &&
                    TryComp(player.AttachedEntity.Value, out RMCSizeComponent? size) &&
                    size.Size != RMCSizes.Humanoid)
                    toRemove.Add(player);
            }

            players.RemovePlayers(toRemove);
            if (_net.IsServer)
                _cameraShake.ShakeCamera(players, ent.Comp.ShakeAmount, ent.Comp.ShakeStrength);
        }

        var ammoEv = new UpdateClientAmmoEvent();
        RaiseLocalEvent(ent, ref ammoEv);
    }
}

[ByRefEvent]
[Serializable, NetSerializable]
public record struct AttemptAirShotEvent(NetEntity Shooter);

[Serializable, NetSerializable]
public sealed partial class AirShotDoAfterEvent : SimpleDoAfterEvent
{
    public readonly NetCoordinates Coordinates;

    public AirShotDoAfterEvent(NetCoordinates coordinates)
    {
        Coordinates = coordinates;
    }
}
