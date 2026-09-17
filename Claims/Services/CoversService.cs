using Claims;
using Covers.Repositories;

/// <summary>
/// Covers Service implementation
/// </summary>
public class CoversService : ICoversService
{
    private readonly ICoversRepository _coversRepository;
    private readonly IAuditService _auditService;

    public CoversService(ICoversRepository coversRepository, IAuditService auditService)
    {
        _coversRepository = coversRepository;
        _auditService = auditService;
    }

    /// <summary>
    /// Gets all covers.
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<Claims.Cover>> GetCoversAsync()
    {
        return await _coversRepository.GetAllAsync();
    }

    /// <summary>
    /// Gets a specific cover by Id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<Cover?> GetCoverAsync(string id)
    {
        return await _coversRepository.GetByIdAsync(id);
    }

    /// <summary>
    /// Creates a new cover.
    /// </summary>
    /// <param name="cover"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<Cover> CreateCoverAsync(Cover cover)
    {
        // Validation Rule: CoverStartDate cannot be in the past
        if (cover.StartDate.Date < DateTime.UtcNow.Date)
        {
            throw new ArgumentException("Cover start date cannot be in the past.");
        }

        // Validation Rule: Total insurance period cannot exceed 1 year
        if (cover.EndDate > cover.StartDate.AddYears(1))
        {
            throw new ArgumentException("Total insurance period cannot exceed 1 year.");
        }

        cover.Id = Guid.NewGuid().ToString();
        cover.Premium = ComputePremium(cover.StartDate, cover.EndDate, cover.Type);
        await _coversRepository.AddAsync(cover);
        await _auditService.AuditCover(cover.Id, "POST");

        return cover;
    }

    /// <summary>
    /// Deletes an existing cover.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task DeleteCoverAsync(string id)
    {
        var cover = await _coversRepository.GetByIdAsync(id);

        if (cover is not null)
        {
            await _coversRepository.DeleteAsync(id);
            await _auditService.AuditCover(id, "DELETE");
        }
    }

    /// <summary>
    /// Computes the total insurance premium based on the cover type and the duration of the insurance period.
    /// </summary>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <param name="coverType"></param>
    /// <returns></returns>
    public decimal ComputePremium(DateTime startDate, DateTime endDate, CoverType coverType)
    {
        // Determine base multiplier using a modern switch expression
        var multiplier = coverType switch
        {
            CoverType.Yacht => 1.1m,
            CoverType.PassengerShip => 1.2m,
            CoverType.Tanker => 1.5m,
            _ => 1.3m // Default for other types
        };

        var basePremiumPerDay = 1250m * multiplier;

        // Use .TotalDays cast to int to get the exact number of billed days
        var totalDays = (int)(endDate - startDate).TotalDays;
        if (totalDays <= 0) return 0m;

        // Determine progressive discount rates
        var isYacht = coverType == CoverType.Yacht;
        var period2Discount = isYacht ? 0.05m : 0.02m;

        // "Additional" 3% for yacht (5+3=8) and 1% for others (2+1=3)
        var period3Discount = isYacht ? 0.08m : 0.03m;

        // Segment the total days into their respective billing periods
        var first30Days = Math.Min(30, totalDays);
        var next150Days = Math.Min(150, Math.Max(0, totalDays - 30));
        var remainingDays = Math.Max(0, totalDays - 180);

        // Calculate total premium without looping
        var totalPremium =
            (first30Days * basePremiumPerDay) +
            (next150Days * basePremiumPerDay * (1m - period2Discount)) +
            (remainingDays * basePremiumPerDay * (1m - period3Discount));

        return totalPremium;
    }    
}