using Content.Server._WH14K.GameTicking.Rules;

namespace Content.Server._WH14K.Altar;

public sealed partial class AltarSystem : EntitySystem
{
    [Dependency] private readonly PlanetaryWarfareRuleSystem _pw = default!;
    public override void Initialize()
    {
        base.Initialize();
    }

    public void AltarExploded(AltarComponent component)
    {
        component.Exploded = true;
        _pw.CheckRoundShouldEnd();
    }
}
