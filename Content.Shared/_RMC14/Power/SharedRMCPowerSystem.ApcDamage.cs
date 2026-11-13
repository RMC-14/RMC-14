using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.ClawSharpness;
using Content.Shared.Damage;
using Content.Shared.Wires;

namespace Content.Shared._RMC14.Power;

public abstract partial class SharedRMCPowerSystem
{
    private void OnApcBeforeDamageChanged(Entity<RMCApcComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (args.Cancelled ||
            !args.Damage.AnyPositive() ||
            args.Origin is not { } attacker ||
            args.Source != attacker ||
            !HasComp<XenoComponent>(attacker) ||
            !HasComp<XenoClawsComponent>(attacker))
        {
            return;
        }

        args.Cancelled = true;
        if (_net.IsClient)
            return;

        if (ent.Comp.Broken)
        {
            _popup.PopupEntity(Loc.GetString("rmc-apc-xeno-already-broken"), ent, attacker);
            return;
        }

        if (ent.Comp.XenoWiresDamaged &&
            (!TryComp(attacker, out RMCSizeComponent? size) || size.Size < ent.Comp.XenoElectronicsDamageSize))
        {
            _popup.PopupEntity(Loc.GetString("rmc-apc-xeno-too-small"), ent, attacker);
            return;
        }

        _audio.PlayPvs(ent.Comp.XenoAttackSound, ent);
        if (ent.Comp.XenoWiresDamaged)
        {
            BreakApc(ent);
            _popup.PopupEntity(Loc.GetString("rmc-apc-xeno-electronics-destroyed"), ent, attacker);
            return;
        }

        if (ent.Comp.WiresExposed)
        {
            SetApcWiresCut(ent, attacker, true);
            ent.Comp.XenoWiresDamaged = true;
            SetAllApcChannels(ent, false);
            ToUpdate.Add(ent);
            _popup.PopupEntity(Loc.GetString("rmc-apc-xeno-wires-cut"), ent, attacker);
            return;
        }

        // CMSS13 checks the previous hit count, exposing wires on the fourth or fifth attack.
        if (ent.Comp.XenoHits >= _random.Next(ent.Comp.XenoExposeWiresMinHits, ent.Comp.XenoExposeWiresMaxHits + 1) &&
            TryComp(ent, out WiresPanelComponent? panel))
        {
            if (_wires.TogglePanel(ent, panel, true))
                _popup.PopupEntity(Loc.GetString("rmc-apc-xeno-wires-exposed"), ent, attacker);
        }
        else
        {
            ent.Comp.XenoHits++;
        }
    }

    private void ResetApcXenoDamage(Entity<RMCApcComponent> ent)
    {
        ent.Comp.XenoHits = 0;
        ent.Comp.XenoWiresDamaged = false;
    }

    protected virtual void SetApcWiresCut(Entity<RMCApcComponent> ent, EntityUid user, bool cut)
    {
    }
}
