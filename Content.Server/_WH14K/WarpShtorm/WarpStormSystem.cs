using Content.Server._WH14K.GameTicking.Rules;

namespace Content.Server._WH14K.WarpShtorm;

public sealed partial class WarpStormSystem : EntitySystem
{
    [Dependency] private readonly PlanetaryWarfareRuleSystem _pw = default!;
    public override void Initialize()
    {
        base.Initialize();
    }

    private void Summon()
    {
        _pw.CheckRoundShouldEnd();
    }
}
