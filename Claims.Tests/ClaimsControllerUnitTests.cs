using Claims.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Claims.Tests;

public class ClaimsControllerUnitTests
{
    private readonly Mock<IClaimsService> _mockClaimService;
    private readonly ClaimsController _controller;

    public ClaimsControllerUnitTests()
    {
        _mockClaimService = new Mock<IClaimsService>();
        _controller = new ClaimsController(_mockClaimService.Object);
    }

    [Fact]
    public async Task GetAsync_ReturnsOkObjectResult_WithClaims()
    {
        // Arrange
        var mockClaims = new List<Claim>
        {
            new Claim { Id = "1", DamageCost = 5000 },
            new Claim { Id = "2", DamageCost = 10000 }
        };
        _mockClaimService.Setup(s => s.GetClaimsAsync()).ReturnsAsync(mockClaims);

        // Act
        var result = await _controller.GetAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsType<List<Claim>>(okResult.Value);
        Assert.Equal(2, returnValue.Count);
    }

    [Fact]
    public async Task CreateAsync_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var invalidClaim = new Claim { DamageCost = 150000 };
        var expectedErrorMessage = "Claim damage cost cannot exceed 100,000.";

        _mockClaimService.Setup(s => s.CreateClaimAsync(It.IsAny<Claim>())).ThrowsAsync(new ArgumentException(expectedErrorMessage));

        // Act
        var result = await _controller.CreateAsync(invalidClaim);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(expectedErrorMessage, badRequestResult.Value);
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedAtAction_WhenSuccessful()
    {
        // Arrange
        var validClaim = new Claim { Id = "123", DamageCost = 50000 };
        _mockClaimService.Setup(s => s.CreateClaimAsync(It.IsAny<Claim>())).ReturnsAsync(validClaim);

        // Act
        var result = await _controller.CreateAsync(validClaim);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(_controller.GetAsync), createdResult.ActionName);
        Assert.Equal("123", createdResult.RouteValues?["id"]);
    }
}