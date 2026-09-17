using Microsoft.AspNetCore.Mvc;

namespace Claims.Controllers;

/// <summary>
/// Handles the insurance claims.
/// </summary>
[ApiController]
[Route("[controller]")]
public class ClaimsController : ControllerBase
{
    private readonly IClaimsService _claimService;

    public ClaimsController(IClaimsService claimService)
    {
        _claimService = claimService;
    }

    /// <summary>
    /// Retrieves all claims.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Claim>>> GetAsync()
    {
        return Ok(await _claimService.GetClaimsAsync());
    }

    /// <summary>
    /// Retrieves a claim by Id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<Claim>> GetAsync(string id)
    {
        var claim = await _claimService.GetClaimAsync(id);
        if (claim is null) return NotFound();
        return Ok(claim);
    }

    /// <summary>
    /// Creates a new claim.
    /// </summary>
    /// <param name="claim"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<ActionResult<Claim>> CreateAsync(Claim claim)
    {
        try
        {
            var createdClaim = await _claimService.CreateClaimAsync(claim);
            return CreatedAtAction(nameof(GetAsync), new { id = createdClaim.Id }, createdClaim);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Deletes a claim by Id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(string id)
    {
        await _claimService.DeleteClaimAsync(id);
        return NoContent();
    }
}