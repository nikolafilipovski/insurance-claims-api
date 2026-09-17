using Covers.Repositories;
using Moq;
using Xunit;

namespace Claims.Tests;

public class CoverServiceTests
{
    private readonly Mock<ICoversRepository> _mockCoversRepository;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly CoversService _coverService;

    public CoverServiceTests()
    {
        _mockCoversRepository = new Mock<ICoversRepository>();
        _mockAuditService = new Mock<IAuditService>();

        _coverService = new CoversService(_mockCoversRepository.Object, _mockAuditService.Object); 
    }

    [Fact]
    public async Task CreateCoverAsync_Throws_WhenStartDateInPast()
    {
        var cover = new Cover
        {
            StartDate = DateTime.UtcNow.Date.AddDays(-1),
            EndDate = DateTime.UtcNow.Date.AddMonths(6),
            Type = CoverType.Yacht
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _coverService.CreateCoverAsync(cover));

        Assert.Contains("cannot be in the past", ex.Message);
    }

    [Fact]
    public async Task CreateCoverAsync_Throws_WhenPeriodExceedsOneYear()
    {
        var start = DateTime.UtcNow.Date.AddDays(1);
        var cover = new Cover
        {
            StartDate = start,
            EndDate = start.AddYears(1).AddDays(1),
            Type = CoverType.Yacht
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _coverService.CreateCoverAsync(cover));

        Assert.Contains("cannot exceed 1 year", ex.Message);
    }

    [Fact]
    public async Task CreateCoverAsync_Allows_ExactlyOneYear()
    {
        var start = DateTime.UtcNow.Date.AddDays(1);
        var cover = new Cover { StartDate = start, EndDate = start.AddYears(1), Type = CoverType.Yacht };

        _mockCoversRepository.Setup(r => r.AddAsync(It.IsAny<Cover>())).Returns(Task.CompletedTask);

        var result = await _coverService.CreateCoverAsync(cover);

        Assert.NotNull(result);
        Assert.True(result.Premium > 0);
    }

    [Fact]
    public async Task CreateCoverAsync_Succeeds_WithValidInput_And_TriggersAudit()
    {
        var cover = new Cover
        {
            StartDate = DateTime.UtcNow.Date.AddDays(1),
            EndDate = DateTime.UtcNow.Date.AddMonths(6),
            Type = CoverType.Yacht
        };
        _mockCoversRepository.Setup(r => r.AddAsync(It.IsAny<Cover>())).Returns(Task.CompletedTask);

        var result = await _coverService.CreateCoverAsync(cover);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Id));
        Assert.True(result.Premium > 0);

        _mockCoversRepository.Verify(r => r.AddAsync(It.IsAny<Cover>()), Times.Once);

        _mockAuditService.Verify(a => a.AuditCover(result.Id, "POST"), Times.Once);
    }

    [Fact]
    public async Task DeleteCoverAsync_CallsRepository_And_TriggersAudit()
    {
        var cover = new Cover { Id = "1" };
        _mockCoversRepository.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(cover);
        _mockCoversRepository.Setup(r => r.DeleteAsync("1")).ReturnsAsync(true);

        await _coverService.DeleteCoverAsync("1");

        _mockCoversRepository.Verify(r => r.DeleteAsync("1"), Times.Once);

        _mockAuditService.Verify(a => a.AuditCover("1", "DELETE"), Times.Once);
    }

    [Theory]
    [InlineData(CoverType.Yacht, 1375)]  
    [InlineData(CoverType.PassengerShip, 1500)]  
    [InlineData(CoverType.Tanker, 1875)]  
    [InlineData(CoverType.ContainerShip, 1625)] 
    public void ComputePremium_First30Days_UsesCorrectBaseRate(CoverType type, decimal expectedDailyRate)
    {
        var start = new DateTime(2025, 1, 1);
        var end = start.AddDays(30);

        var premium = _coverService.ComputePremium(start, end, type);

        Assert.Equal(30 * expectedDailyRate, premium);
    }

    [Fact]
    public void ComputePremium_Yacht_Applies5PercentDiscountForNext150Days()
    {
        var start = new DateTime(2025, 1, 1);
        var end = start.AddDays(180); 

        var premium = _coverService.ComputePremium(start, end, CoverType.Yacht);

        var expected = (30 * 1375m) + (150 * 1375m * 0.95m);
        Assert.Equal(expected, premium);
    }

    [Fact]
    public void ComputePremium_Other_Applies2PercentDiscountForNext150Days()
    {
        var start = new DateTime(2025, 1, 1);
        var end = start.AddDays(180);

        var premium = _coverService.ComputePremium(start, end, CoverType.BulkCarrier);

        var expected = (30 * 1625m) + (150 * 1625m * 0.98m);
        Assert.Equal(expected, premium);
    }

    [Fact]
    public void ComputePremium_Yacht_Applies8PercentDiscountForRemainingDays()
    {
        var start = new DateTime(2025, 1, 1);
        var end = start.AddDays(365); 

        var premium = _coverService.ComputePremium(start, end, CoverType.Yacht);

        var expected = (30 * 1375m) + (150 * 1375m * 0.95m) + (185 * 1375m * 0.92m);
        Assert.Equal(expected, premium);
    }

    [Fact]
    public void ComputePremium_Other_Applies3PercentDiscountForRemainingDays()
    {
        var start = new DateTime(2025, 1, 1);
        var end = start.AddDays(365);

        var premium = _coverService.ComputePremium(start, end, CoverType.ContainerShip);

        var expected = (30 * 1625m) + (150 * 1625m * 0.98m) + (185 * 1625m * 0.97m);
        Assert.Equal(expected, premium);
    }

    [Fact]
    public void ComputePremium_ReturnsZero_WhenPeriodIsZeroOrNegative()
    {
        var start = new DateTime(2025, 1, 1);

        Assert.Equal(0m, _coverService.ComputePremium(start, start, CoverType.Yacht));
        Assert.Equal(0m, _coverService.ComputePremium(start, start.AddDays(-5), CoverType.Yacht));
    }
}