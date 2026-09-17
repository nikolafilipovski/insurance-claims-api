using Claims;
using Claims.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Covers.Repositories;

/// <summary>
/// Covers Repository implementation
/// </summary>
public class CoversRepository : ICoversRepository
{
    private readonly ClaimsContext _context;

    public CoversRepository(ClaimsContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all covers.
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<Cover>> GetAllAsync()
    {
        return await _context.Covers.ToListAsync();
    }

    /// <summary>
    /// Gets a cover by it's Id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<Cover?> GetByIdAsync(string id)
    {
        return await _context.Covers.SingleOrDefaultAsync(cover => cover.Id == id);
    }

    /// <summary>
    /// Adds a new cover.
    /// </summary>
    /// <param name="cover"></param>
    /// <returns></returns>
    public async Task AddAsync(Cover cover)
    {
        _context.Covers.Add(cover);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes a specific cover.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<bool> DeleteAsync(string id)
    {
        var cover = await GetByIdAsync(id);
        if (cover is null)
        {
            return false;
        }

        _context.Covers.Remove(cover);
        await _context.SaveChangesAsync();
        return true;
    }
}