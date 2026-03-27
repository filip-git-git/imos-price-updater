using IMOS.PriceUpdater.Models;

namespace IMOS.PriceUpdater.Tests.Models;

public class ProgressEventTests
{
    [Fact]
    public void Percentage_ZeroProgress_ReturnsZero()
    {
        // Act
        var progress = new ProgressEvent(0, 100);

        // Assert
        Assert.Equal(0, progress.Percentage);
    }

    [Fact]
    public void Percentage_HalfProgress_ReturnsFifty()
    {
        // Act
        var progress = new ProgressEvent(50, 100);

        // Assert
        Assert.Equal(50, progress.Percentage);
    }

    [Fact]
    public void Percentage_CompleteProgress_ReturnsHundred()
    {
        // Act
        var progress = new ProgressEvent(100, 100);

        // Assert
        Assert.Equal(100, progress.Percentage);
    }

    [Fact]
    public void Percentage_ZeroTotalRows_ReturnsZero()
    {
        // Act
        var progress = new ProgressEvent(50, 0);

        // Assert
        Assert.Equal(0, progress.Percentage);
    }

    [Fact]
    public void Percentage_PartialProgress_CalculatesCorrectly()
    {
        // Act
        var progress = new ProgressEvent(33, 100);

        // Assert
        Assert.Equal(33, progress.Percentage);
    }

    [Fact]
    public void IsComplete_NotFinished_ReturnsFalse()
    {
        // Act
        var progress = new ProgressEvent(50, 100);

        // Assert
        Assert.False(progress.IsComplete);
    }

    [Fact]
    public void IsComplete_Finished_ReturnsTrue()
    {
        // Act
        var progress = new ProgressEvent(100, 100);

        // Assert
        Assert.True(progress.IsComplete);
    }

    [Fact]
    public void IsComplete_OverFinished_ReturnsTrue()
    {
        // Act
        var progress = new ProgressEvent(150, 100);

        // Assert
        Assert.True(progress.IsComplete);
    }

    [Fact]
    public void ProgressEvent_WithMessage_ContainsMessage()
    {
        // Act
        var progress = new ProgressEvent(50, 100, "Processing row 50");

        // Assert
        Assert.Equal("Processing row 50", progress.Message);
    }

    [Fact]
    public void ProgressEvent_WithoutMessage_NullMessage()
    {
        // Act
        var progress = new ProgressEvent(50, 100);

        // Assert
        Assert.Null(progress.Message);
    }
}

