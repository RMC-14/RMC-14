using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Content.IntegrationTests._RMC14;

[TestFixture]
public sealed class RMCWeatherMapDataTest
{
    [Test]
    public void RMCWeatherCyclesHaveGameplayData()
    {
        foreach (var file in WeatherMapFiles())
        {
            var blocks = ExtractWeatherCycleBlocks(File.ReadAllLines(file));
            Assert.That(blocks, Is.Not.Empty, $"{file} should contain an RMC weather cycle");

            foreach (var block in blocks)
            {
                var text = string.Join('\n', block);
                Assert.Multiple(() =>
                {
                    Assert.That(text, Does.Contain("minTimeBetweenChecks:"), $"{file} should define weather check cadence");
                    Assert.That(text, Does.Contain("minCheckVariance:"), $"{file} should define weather check variance");
                    Assert.That(text, Does.Contain("startChance:"), $"{file} should define weather start chance");
                    Assert.That(text, Does.Not.Contain("durationRemaining:"), $"{file} must not serialize runtime duration state");
                    Assert.That(text, Does.Not.Contain("RMCBigRedRocks"), $"{file} must not enable Big Red rockstorms by accident");
                });

                var weatherTypeCount = block.Count(line => line.Contains("weatherType:"));
                var smotherCount = block.Count(line => line.Contains("fireSmotheringStrength:"));
                Assert.That(smotherCount, Is.EqualTo(weatherTypeCount), $"{file} should give every weather event gameplay smothering data");
            }

            var fileName = Path.GetFileName(file);
            var fullText = string.Join('\n', blocks.SelectMany(block => block));
            if (fileName.Contains("varadero"))
                Assert.That(fullText, Does.Contain("weather_warning_varadero.ogg"), $"{file} should use the Varadero storm siren");

            if (fileName.Contains("sorokyne"))
                Assert.That(fullText, Does.Contain("weather_warning.ogg"), $"{file} should use the Sorokyne monsoon siren");
        }
    }

    private static IEnumerable<string> WeatherMapFiles()
    {
        var rootDir = Path.Join(Directory.GetParent(Directory.GetCurrentDirectory())!.Parent!.ToString());
        var mapsDir = Path.Combine(rootDir, "Resources", "Maps", "_RMC14");
        return Directory.GetFiles(mapsDir, "*.yml", SearchOption.AllDirectories)
            .Where(file => File.ReadAllText(file).Contains("RMCWeatherCycle"));
    }

    private static List<List<string>> ExtractWeatherCycleBlocks(string[] lines)
    {
        var blocks = new List<List<string>>();
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i] != "    - type: RMCWeatherCycle")
                continue;

            var block = new List<string>();
            for (var j = i; j < lines.Length; j++)
            {
                if (j > i && (lines[j].StartsWith("    - type: ") || lines[j].StartsWith("  - uid: ")))
                    break;

                block.Add(lines[j]);
            }

            blocks.Add(block);
        }

        return blocks;
    }
}
