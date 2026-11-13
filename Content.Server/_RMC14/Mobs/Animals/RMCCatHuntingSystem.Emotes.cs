using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCCatHuntingSystem
{
    private void TryMeow(Entity<RMCCatHunterComponent> ent, TimeSpan now)
    {
        if (ent.Comp.NextMeowAt > now)
            return;

        ent.Comp.NextMeowAt = now + RandomTime(ent.Comp.MeowCooldownMin, ent.Comp.MeowCooldownMax);
        _audio.PlayPvs(ent.Comp.MeowSound, ent.Owner);
    }

    private void TryAmbientCatEmote(Entity<RMCCatHunterComponent> ent, TimeSpan now)
    {
        if (ActorQuery.HasComp(ent.Owner) ||
            ent.Comp.NextAmbientEmoteAt > now)
        {
            return;
        }

        if (Random.Prob(ent.Comp.HeardEmoteChance))
        {
            ent.Comp.NextAmbientEmoteAt = now + ent.Comp.AmbientEmoteCooldown;
            Popup.PopupEntity(Loc.GetString(PickCatHeardEmote(), ("cat", ent.Owner)), ent.Owner);
            return;
        }

        if (!Random.Prob(ent.Comp.SeenEmoteChance))
            return;

        ent.Comp.NextAmbientEmoteAt = now + ent.Comp.AmbientEmoteCooldown;
        Popup.PopupEntity(Loc.GetString(PickCatSeenEmote(), ("cat", ent.Owner)), ent.Owner);
    }

    private string PickCatHeardEmote()
    {
        return Random.Next(3) switch
        {
            0 => "rmc-cat-meows",
            1 => "rmc-cat-mews",
            _ => "rmc-cat-mrrps",
        };
    }

    private string PickCatSeenEmote()
    {
        return Random.Next(3) switch
        {
            0 => "rmc-cat-shakes-head",
            1 => "rmc-cat-shivers",
            _ => "rmc-cat-licks-paw",
        };
    }
}
