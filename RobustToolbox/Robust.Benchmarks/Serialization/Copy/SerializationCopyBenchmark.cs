﻿using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Robust.Benchmarks.Serialization.Definitions;
using Robust.Shared.Analyzers;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Benchmarks.Serialization.Copy
{
    [MemoryDiagnoser]
    [Virtual]
    public class SerializationCopyBenchmark : SerializationBenchmark
    {
        public SerializationCopyBenchmark()
        {
            InitializeSerialization();

            DataDefinitionWithString = new DataDefinitionWithString {StringField = "ABC"};

            var yamlStream = new YamlStream();
            yamlStream.Load(new StringReader(SeedDataDefinition.Prototype));

            var seedMapping = yamlStream.Documents[0].RootNode.ToDataNodeCast<SequenceDataNode>().Cast<MappingDataNode>(0);

            Seed = SerializationManager.Read<SeedDataDefinition>(seedMapping, notNullableOverride: true);
        }

        private const string String = "ABC";

        private const int Integer = 1;

        private DataDefinitionWithString DataDefinitionWithString { get; }

        private SeedDataDefinition Seed { get; }

        private BenchmarkFlagsEnum FlagZero = BenchmarkFlagsEnum.Zero;

        private BenchmarkFlagsEnum FlagThirtyOne = BenchmarkFlagsEnum.ThirtyOne;

        [Benchmark]
        public string? CreateCopyString()
        {
            return SerializationManager.CreateCopy(String, notNullableOverride: true);
        }

        [Benchmark]
        public int? CreateCopyInteger()
        {
            return SerializationManager.CreateCopy(Integer);
        }

        [Benchmark]
        public DataDefinitionWithString? CreateCopyDataDefinitionWithString()
        {
            return SerializationManager.CreateCopy(DataDefinitionWithString, notNullableOverride: true);
        }

        [Benchmark]
        public SeedDataDefinition? CreateCopySeedDataDefinition()
        {
            return SerializationManager.CreateCopy(Seed, notNullableOverride: true);
        }

        [Benchmark]
        public SeedDataDefinition BaselineCreateCopySeedDataDefinition()
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            var copy = new SeedDataDefinition();

            copy.ID = Seed.ID;
            copy.Name = Seed.Name;
            copy.SeedName = Seed.SeedName;
            copy.SeedNoun = Seed.SeedNoun;
            copy.DisplayName = Seed.DisplayName;
            copy.RoundStart = Seed.RoundStart;
            copy.Mysterious = Seed.Mysterious;
            copy.Immutable = Seed.Immutable;

            copy.ProductPrototypes = Seed.ProductPrototypes.ToList();
            copy.Chemicals = Seed.Chemicals.ToDictionary(p => p.Key, p => p.Value);
            copy.ConsumeGasses = Seed.ConsumeGasses.ToDictionary(p => p.Key, p => p.Value);
            copy.ExudeGasses = Seed.ExudeGasses.ToDictionary(p => p.Key, p => p.Value);

            copy.NutrientConsumption = Seed.NutrientConsumption;
            copy.WaterConsumption = Seed.WaterConsumption;
            copy.IdealHeat = Seed.IdealHeat;
            copy.HeatTolerance = Seed.HeatTolerance;
            copy.IdealLight = Seed.IdealLight;
            copy.LightTolerance = Seed.LightTolerance;
            copy.ToxinsTolerance = Seed.ToxinsTolerance;
            copy.LowPressureTolerance = Seed.LowPressureTolerance;
            copy.HighPressureTolerance = Seed.HighPressureTolerance;
            copy.PestTolerance = Seed.PestTolerance;
            copy.WeedTolerance = Seed.WeedTolerance;

            copy.Endurance = Seed.Endurance;
            copy.Yield = Seed.Yield;
            copy.Lifespan = Seed.Lifespan;
            copy.Maturation = Seed.Maturation;
            copy.Production = Seed.Production;
            copy.GrowthStages = Seed.GrowthStages;
            copy.HarvestRepeat = Seed.HarvestRepeat;
            copy.Potency = Seed.Potency;
            copy.Ligneous = Seed.Ligneous;

            copy.PlantRsi = new ResPath(Seed.PlantRsi.ToString());
            copy.PlantIconState = Seed.PlantIconState;
            copy.Bioluminescent = Seed.Bioluminescent;
            copy.BioluminescentColor = Seed.BioluminescentColor;
            copy.SplatPrototype = Seed.SplatPrototype;

            return copy;
        }

        [Benchmark]
        [BenchmarkCategory("flag")]
        public object? CopyFlagZero()
        {
            return SerializationManager.CreateCopy<int, FlagSerializer<BenchmarkFlags>>((int) FlagZero);
        }

        [Benchmark]
        [BenchmarkCategory("flag")]
        public object? CopyFlagThirtyOne()
        {
            return SerializationManager.CreateCopy<int, FlagSerializer<BenchmarkFlags>>((int) FlagThirtyOne);
        }

        [Benchmark]
        [BenchmarkCategory("customTypeSerializer")]
        public object? CopyIntegerCustomSerializer()
        {
            return SerializationManager.CreateCopy<int, BenchmarkIntSerializer>(Integer);
        }
    }
}
