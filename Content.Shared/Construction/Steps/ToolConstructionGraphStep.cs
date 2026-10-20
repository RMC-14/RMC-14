using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Tools;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;    // RMC14
using System.Linq;

namespace Content.Shared.Construction.Steps
{
    [DataDefinition]
    public sealed partial class ToolConstructionGraphStep : ConstructionGraphStep
    {
        // Begin RMC14
        [DataField("tools", required:true, customTypeSerializer:typeof(PrototypeIdListSerializer<ToolQualityPrototype>))]
        public List<string> Tools { get; private set; } = new();
        // End RMC14

        [DataField("fuel")] public float Fuel { get; private set; } = 10;

        [DataField("examine")] public string ExamineOverride { get; private set; } = string.Empty;

        // Begin RMC14
        [DataField] public DuplicateConditions DuplicateConditions { get; private set; }

        public override void DoExamine(ExaminedEvent examinedEvent)
        {
            if (!string.IsNullOrEmpty(ExamineOverride))
            {
                examinedEvent.PushMarkup(Loc.GetString(ExamineOverride));
                return;
            }

            var prototype = IoCManager.Resolve<IPrototypeManager>();

            foreach (var tool in Tools)
            {
                if (!prototype.TryIndex(tool, out ToolQualityPrototype? quality))
                    continue;

                examinedEvent.PushMarkup(Loc.GetString("construction-use-tool-entity", ("toolName", Loc.GetString(quality.ToolName))));
            }
        }

        public override ConstructionGuideEntry GenerateGuideEntry()
        {
            var prototype = IoCManager.Resolve<IPrototypeManager>();

            var qualities = Tools.Select(tool => prototype.Index<ToolQualityPrototype>(tool)).ToList();
            
            var names = qualities.Select(quality => Loc.GetString(quality.ToolName)).ToList();

            return new ConstructionGuideEntry()
            {
                Localization = names.Count == 1 ? "construction-presenter-tool-step":"construction-presenter-tool-step-multiple",
                Arguments = new (string, object)[] { ("tools", string.Join(", ", names)) },
                Icon = qualities[0].Icon,
            };
        }
    }
}
// End RMC14
