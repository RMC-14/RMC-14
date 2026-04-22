using System;
using Content.Shared._RMC14.UniversalRecorder;
using NUnit.Framework;

namespace Content.Tests.Shared._RMC14.UniversalRecorder;

[TestFixture]
[TestOf(typeof(UniversalRecorderFormatting))]
public sealed class UniversalRecorderFormattingTests : ContentUnitTest
{
    [Test]
    public void TimestampFormattingUsesMinuteSecondFormat()
    {
        var formatted = UniversalRecorderFormatting.FormatTimestamp(TimeSpan.FromMinutes(3) + TimeSpan.FromSeconds(7));

        Assert.That(formatted, Is.EqualTo("[03:07]"));
    }

    [Test]
    public void TranscriptFormattingIncludesSpeakerAndQuotedText()
    {
        var formatted = UniversalRecorderFormatting.FormatTranscriptLine(
            TimeSpan.FromMinutes(12) + TimeSpan.FromSeconds(5),
            "Provost",
            "asks",
            "State your name for the record.");

        Assert.That(formatted, Is.EqualTo("[12:05] Provost asks, \"State your name for the record.\""));
    }

    [Test]
    public void TapeConfigDefaultsMatchExpectedValues()
    {
        var tape = new UniversalRecorderTapeComponent();

        Assert.That(tape.MaxCapacity, Is.EqualTo(TimeSpan.FromMinutes(20)));
        Assert.That(tape.RespoolTime, Is.EqualTo(TimeSpan.FromSeconds(5)));
        Assert.That(tape.ScrewdriverQuality, Is.EqualTo("Screwing"));
    }
}
