using ZEGU.Core.Enums;
using ZEGU.Infrastructure.Services;

namespace ZEGU.Tests;

public class RequestPriorityClassifierTests
{
    [Fact]
    public void Determine_ReturnsCategoryDefault_WhenNoUrgencyKeywordsPresent()
    {
        var result = RequestPriorityClassifier.Determine(RequestPriority.Low, "Squeaky chair", "The chair in room 204 squeaks when you sit on it.");

        Assert.Equal(RequestPriority.Low, result);
    }

    [Theory]
    [InlineData("Fire in the lab", "There is smoke coming from the equipment.")]
    [InlineData("Gas smell", "I can smell gas near the kitchen.")]
    [InlineData("Exposed wire", "There is a live wire hanging from the ceiling.")]
    public void Determine_EscalatesToEmergency_WhenEmergencyKeywordPresent(string title, string description)
    {
        var result = RequestPriorityClassifier.Determine(RequestPriority.Low, title, description);

        Assert.Equal(RequestPriority.Emergency, result);
    }

    [Theory]
    [InlineData("No power", "The whole floor has no electricity since this morning.")]
    [InlineData("Leaking pipe", "There is water leaking under the sink.")]
    public void Determine_EscalatesToHigh_WhenHighKeywordPresent(string title, string description)
    {
        var result = RequestPriorityClassifier.Determine(RequestPriority.Low, title, description);

        Assert.Equal(RequestPriority.High, result);
    }

    [Fact]
    public void Determine_NeverLowersBelowCategoryDefault()
    {
        // Category default is already Emergency; ordinary text should not pull it down to Normal.
        var result = RequestPriorityClassifier.Determine(RequestPriority.Emergency, "Squeaky chair", "The chair squeaks.");

        Assert.Equal(RequestPriority.Emergency, result);
    }

    [Fact]
    public void Determine_IgnoresSelfReportedUrgencyWithNoRealSignal()
    {
        // The whole point: saying "urgent" or "critical" with no actual hazard described
        // must not move the needle - only real keywords do.
        var result = RequestPriorityClassifier.Determine(RequestPriority.Low, "URGENT CRITICAL EMERGENCY", "Please fix my chair, it is very urgent and critical.");

        Assert.Equal(RequestPriority.Low, result);
    }

    [Fact]
    public void Determine_IsCaseInsensitive()
    {
        var result = RequestPriorityClassifier.Determine(RequestPriority.Low, "FIRE", "SMOKE EVERYWHERE");

        Assert.Equal(RequestPriority.Emergency, result);
    }

    [Fact]
    public void Determine_TakesHigherOfCategoryDefaultAndKeywordEscalation()
    {
        // Category default (High) already exceeds what the keyword alone would suggest (also High) - stays High.
        var result = RequestPriorityClassifier.Determine(RequestPriority.High, "No power", "No electricity in the lab.");

        Assert.Equal(RequestPriority.High, result);
    }
}
