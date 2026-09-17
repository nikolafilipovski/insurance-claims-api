using Claims;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Handles operations for insurance covers.
/// </summary>
[ApiController]
[Route("[controller]")]
public class CoversController : ControllerBase
{
    private readonly ICoversService _coverService;

    public CoversController(ICoversService coverService)
    {
        _coverService = coverService;
    }

    /// <summary>
    /// Computes the premium for a cover period.
    /// </summary>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <param name="coverType"></param>
    /// <returns></returns>
    [HttpPost("compute")]
    public ActionResult<decimal> ComputePremiumAsync(DateTime startDate, DateTime endDate, CoverType coverType)
    {
        return Ok(_coverService.ComputePremium(startDate, endDate, coverType));
    }

    /// <summary>
    /// Retrieves all covers.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Cover>>> GetAsync()
    {
        var results = await _coverService.GetCoversAsync();
        return Ok(results);
    }

    /// <summary>
    /// Retrieves a specific cover by Id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<Cover>> GetAsync(string id)
    {
        var cover = await _coverService.GetCoverAsync(id);
        if (cover is null) return NotFound();
        return Ok(cover);
    }

    /// <summary>
    /// Creates a new cover.
    /// </summary>
    /// <param name="cover"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<ActionResult<Cover>> CreateAsync(Cover cover)
    {
        try
        {
            var createdCover = await _coverService.CreateCoverAsync(cover);
            return CreatedAtAction(nameof(GetAsync), new { id = createdCover.Id }, createdCover);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Deletes a cover by Id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(string id)
    {
        await _coverService.DeleteCoverAsync(id);
        return NoContent();
    }
}