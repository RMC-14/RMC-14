using System.Linq;
using Content.Shared._RMC14.Rules;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Server._RMC14.Toolshed;

public sealed class EntProtoIdTTypeParser<T> : TypeParser<EntProtoId<T>> where T : IComponent, new()
{
    public override bool TryParse(ParserContext ctx, out EntProtoId<T> result)
    {
        result = default;
        if (!Toolshed.TryParse(ctx, out ProtoId<EntityPrototype> proto))
            return false;

        result = new EntProtoId<T>(proto.Id);
        return true;
    }

    public override CompletionResult? TryAutocomplete(ParserContext parserContext, CommandArgument? arg)
    {
        var entities = IoCManager.Resolve<IEntityManager>();
        var planet = entities.System<RMCPlanetSystem>();
        return CompletionResult.FromHintOptions(planet.PlanetPaths.Values.Select(e => e.Id), "id");
    }
}
