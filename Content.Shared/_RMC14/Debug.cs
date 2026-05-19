using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.Console;

namespace Content.Shared._RMC14;

[UsedImplicitly]
internal sealed class EntityComparisonCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override string Command => "entcompare";
    public override string Description => "Compares two entities, displaying differences in components";
    public override string Help => "Usage: entcompare <entity uid A> <entity uid B>";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteLine(Help);
            return;
        }

        if (!NetEntity.TryParse(args[0], out var neteidA) ||
            !EntityManager.TryGetEntity(neteidA, out var rA) ||
            rA is not { } eidA)
        {
            shell.WriteError("EID A not valid (either malformed or doesn't exist).");
            return;
        }

        if (!NetEntity.TryParse(args[1], out var neteidB) ||
            !EntityManager.TryGetEntity(neteidB, out var rB) ||
            rB is not { } eidB)
        {
            shell.WriteError("EID B not valid (either malformed or doesn't exist).");
            return;
        }

        var metaA = EntityManager.GetComponent<MetaDataComponent>(eidA);
        var metaB = EntityManager.GetComponent<MetaDataComponent>(eidB);
        if (metaA.EntityName != metaB.EntityName)
        {
            shell.WriteLine("MetaData.EntityName difference:");
            shell.WriteLine($"    eidA: {metaA.EntityName}");
            shell.WriteLine($"    eidB: {metaB.EntityName}");
            shell.WriteLine("");
        }
        if (metaA.EntityPrototype?.ID != metaB.EntityPrototype?.ID)
        {
            shell.WriteLine("MetaData.EntityPrototype.ID difference:");
            shell.WriteLine($"    eidA: {metaA.EntityPrototype?.ID}");
            shell.WriteLine($"    eidB: {metaB.EntityPrototype?.ID}");
            shell.WriteLine("");
        }

        Dictionary<string, IComponent> compsA = new Dictionary<string, IComponent>();
        Dictionary<string, IComponent> compsB = new Dictionary<string, IComponent>();
        foreach (var component in EntityManager.GetComponents(eidA))
        {
            compsA[component.ToString() ?? "Unknown"] = component;
        }
        foreach (var component in EntityManager.GetComponents(eidB))
        {
            compsB[component.ToString() ?? "Unknown"] = component;
        }
        var onlyAComps = compsA.Keys.Except(compsB.Keys);
        var onlyBComps = compsB.Keys.Except(compsA.Keys);
        var bothComps = compsA.Keys.Intersect(compsB.Keys);

        if (onlyAComps.Count() > 0)
        {
            shell.WriteLine($"eidA ({metaA.EntityName}) contains these extra components:");
            foreach (var comp in onlyAComps)
            {
                shell.WriteLine($"   {comp}");
            }
            shell.WriteLine("");
        }
        if (onlyBComps.Count() > 0)
        {
            shell.WriteLine($"eidB ({metaB.EntityName}) contains these extra components:");
            foreach (var comp in onlyBComps)
            {
                shell.WriteLine($"   {comp}");
            }
            shell.WriteLine("");
        }
        foreach (var compName in bothComps)
        {
            var compA = compsA[compName];
            var compB = compsB[compName];
            if (compA is IComponentDebug compADebug && compB is IComponentDebug compBDebug)
            {
                var a_string = compADebug.GetDebugString();
                var b_string = compBDebug.GetDebugString();

                if (a_string != b_string)
                {
                    shell.WriteLine($"DIFFERENCES IN {compName}:");
                    shell.WriteLine($"  A debug string:");
                    foreach (var line in SplitToLines(a_string))
                    {
                        shell.WriteLine($"    {line}");
                    }
                    shell.WriteLine($"  B debug string:");
                    foreach (var line in SplitToLines(b_string))
                    {
                        shell.WriteLine($"    {line}");
                    }
                }
            }
        }
    }

    private static IEnumerable<string> SplitToLines(string input)
    {
        int start = 0;

        for (int i = 0; i < input.Length; i++)
        {
            switch (input[i])
            {
                case '\r':
                    yield return input[start..i];

                    // Treat \r\n as a single newline
                    if (i + 1 < input.Length && input[i + 1] == '\n')
                        i++;

                    start = i + 1;
                    break;

                case '\n':
                    yield return input[start..i];
                    start = i + 1;
                    break;
            }
        }

        // Final line
        if (start < input.Length)
            yield return input[start..];
    }
}
