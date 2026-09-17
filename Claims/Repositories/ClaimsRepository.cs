namespace Claims.Repositories;

/// <summary>
/// Claims Repository implementation
/// </summary>
public class ClaimsRepository : IClaimsRepository
{
    private readonly ClaimsContext _context;

    public ClaimsRepository(ClaimsContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all claims.
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<Claim>> GetAllAsync()
    {
        return await _context.GetClaimsAsync();
    }

    /// <summary>
    /// Gets a claim by Id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<Claim?> GetByIdAsync(string id)
    {
        return await _context.GetClaimAsync(id);
    }

    /// <summary>
    /// Adds a new claim.
    /// </summary>
    /// <param name="claim"></param>
    /// <returns></returns>
    public async Task AddAsync(Claim claim)
    {
        _context.AddAsync(claim);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes a specific claim.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<bool> DeleteAsync(string id)
    {
        var claim = await GetByIdAsync(id);
        
        if (claim is null)
        {
            return false;
        }

        _context.Remove(claim);
        await _context.SaveChangesAsync();
        return true;
    }
}