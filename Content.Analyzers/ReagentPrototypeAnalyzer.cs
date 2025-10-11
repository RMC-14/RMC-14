using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Content.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ReagentPrototypeAnalyzer : DiagnosticAnalyzer
{
    [SuppressMessage("ReSharper", "RS2008")]
    private static readonly DiagnosticDescriptor Rule = new(
        Diagnostics.IdReagentPrototype,
        "Reagent prototypes must be resolved through ReagentSystem, not IPrototypeManager",
        "Reagent prototypes must be resolved through ReagentSystem, not IPrototypeManager",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Reagent prototypes must be resolved through ReagentSystem, not IPrototypeManager.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(compilationCtx =>
        {
            var indexMethods = compilationCtx.Compilation
                .GetTypeByMetadataName("Robust.Shared.Prototypes.IPrototypeManager")?
                .GetMembers()
                .Where(m => m.Name is "Index" or "TryIndex")
                .Cast<IMethodSymbol>();

            var reagentPrototype = compilationCtx.Compilation.GetTypeByMetadataName("Content.Shared.Chemistry.Reagent.ReagentPrototype");

            if (indexMethods == null)
                return;

            var indexMethodsArray = indexMethods.ToImmutableArray();
            compilationCtx.RegisterOperationAction(ctx => AnalyzeNode(ctx, indexMethodsArray, reagentPrototype), OperationKind.Invocation);
        });
    }

    private static void AnalyzeNode(OperationAnalysisContext context, ImmutableArray<IMethodSymbol> methods, INamedTypeSymbol reagentPrototype)
    {
        if (context.Operation is not IInvocationOperation operation)
            return;

        if (!operation.TargetMethod.Name.Contains("Index"))
            return;

        if (!methods.Any(m => m.Equals(operation.TargetMethod.OriginalDefinition, SymbolEqualityComparer.Default)))
        {
            return;
        }

        if (SymbolEqualityComparer.Default.Equals(operation.Type, reagentPrototype))
        {
            var diagnostic = Diagnostic.Create(Rule, operation.Syntax.GetLocation());
            context.ReportDiagnostic(diagnostic);
            return;
        }

        foreach (var argument in operation.Arguments)
        {
            if (!SymbolEqualityComparer.Default.Equals(argument.Value.Type, reagentPrototype))
                continue;

            var diagnostic = Diagnostic.Create(Rule, argument.Syntax.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
