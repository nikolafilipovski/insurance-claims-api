using Claims.Repositories;
using Covers.Repositories;
using Moq;
using Xunit;

namespace Claims.Tests;

public class ClaimServiceTests
{
    private readonly Mock<IClaimsRepository> _mockClaimsRepository;
    private readonly Mock<ICoversRepository> _mockCoversRepository;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly ClaimsService _claimService;

    public ClaimServiceTests()
    {
        _mockClaimsRepository = new Mock<IClaimsRepository>();
        _mockCoversRepository = new Mock<ICoversRepository>();
        _mockAuditService = new Mock<IAuditService>();

        _claimService = new ClaimsService(
            _mockClaimsRepository.Object,
            _mockCoversRepository.Object,
            _mockAuditService.Object); 
    }

    [Fact]
    public async Task CreateClaimAsync_Throws_WhenDamageCostExceeds100000()
    {
        var claim = new Claim { DamageCost = 100_001, CoverId = "cover-1" };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _claimService.CreateClaimAsync(claim));

        Assert.Contains("100,000", ex.Message);
    }

    [Fact]
    public async Task CreateClaimAsync_Allows_Exact100000()
    {
        var cover = new Cover { Id = "cover-1", StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2025, 6, 1) };
        var claim = new Claim { DamageCost = 100_000, CoverId = "cover-1", Created = new DateTime(2025, 3, 1) };

        _mockCoversRepository.Setup(r => r.GetByIdAsync("cover-1")).ReturnsAsync(cover);
        _mockClaimsRepository.Setup(r => r.AddAsync(It.IsAny<Claim>())).Returns(Task.CompletedTask);

        var result = await _claimService.CreateClaimAsync(claim);

        Assert.Equal(100_000, result.DamageCost);
    }

    [Fact]
    public async Task CreateClaimAsync_Throws_WhenRelatedCoverDoesNotExist()
    {
        var claim = new Claim { DamageCost = 5_000, CoverId = "non-existent" };
        _mockCoversRepository.Setup(r => r.GetByIdAsync("non-existent")).ReturnsAsync((Cover?)null);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _claimService.CreateClaimAsync(claim));

        Assert.Contains("does not exist", ex.Message);
    }

    [Fact]
    public async Task CreateClaimAsync_Throws_WhenCreatedDateBeforeCoverStart()
    {
        var cover = new Cover { Id = "cover-1", StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2025, 6, 1) };
        var claim = new Claim { DamageCost = 5_000, CoverId = "cover-1", Created = new DateTime(2024, 12, 31) };

        _mockCoversRepository.Setup(r => r.GetByIdAsync("cover-1")).ReturnsAsync(cover);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _claimService.CreateClaimAsync(claim));

        Assert.Contains("within the related cover", ex.Message);
    }

    [Fact]
    public async Task CreateClaimAsync_Throws_WhenCreatedDateAfterCoverEnd()
    {
        var cover = new Cover { Id = "cover-1", StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2025, 6, 1) };
        var claim = new Claim { DamageCost = 5_000, CoverId = "cover-1", Created = new DateTime(2025, 6, 2) };

        _mockCoversRepository.Setup(r => r.GetByIdAsync("cover-1")).ReturnsAsync(cover);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _claimService.CreateClaimAsync(claim));

        Assert.Contains("within the related cover", ex.Message);
    }

    [Fact]
    public async Task CreateClaimAsync_Succeeds_WithValidInput_And_TriggersAudit()
    {
        var cover = new Cover { Id = "cover-1", StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2025, 6, 1) };
        var claim = new Claim { DamageCost = 5_000, CoverId = "cover-1", Created = new DateTime(2025, 3, 1) };

        _mockCoversRepository.Setup(r => r.GetByIdAsync("cover-1")).ReturnsAsync(cover);
        _mockClaimsRepository.Setup(r => r.AddAsync(It.IsAny<Claim>())).Returns(Task.CompletedTask);

        var result = await _claimService.CreateClaimAsync(claim);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Id));
        _mockClaimsRepository.Verify(r => r.AddAsync(It.IsAny<Claim>()), Times.Once);

        _mockAuditService.Verify(a => a.AuditClaim(result.Id, "POST"), Times.Once);
    }

    [Fact]
    public async Task DeleteClaimAsync_CallsRepository_And_TriggersAudit()
    {
        _mockClaimsRepository.Setup(r => r.DeleteAsync("1")).ReturnsAsync(true);

        await _claimService.DeleteClaimAsync("1");

        _mockClaimsRepository.Verify(r => r.DeleteAsync("1"), Times.Once);

        _mockAuditService.Verify(a => a.AuditClaim("1", "DELETE"), Times.Once);
    }
}