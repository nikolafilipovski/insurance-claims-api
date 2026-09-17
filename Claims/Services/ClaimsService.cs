using Claims;
using Claims.Repositories;
using Covers.Repositories;

/// <summary>
/// Claims Service implemetation
/// </summary>
public class ClaimsService : IClaimsService
{
    private readonly IClaimsRepository _claimsRepository;
    private readonly ICoversRepository _coversRepository;
    private readonly IAuditService _auditService;

    public ClaimsService(IClaimsRepository claimsRepository, ICoversRepository coversRepository, IAuditService auditService)
    {
        _claimsRepository = claimsRepository;
        _coversRepository = coversRepository;
        _auditService = auditService;
    }

    /// <summary>
    /// Gets all claims.
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<Claim>> GetClaimsAsync() => await _claimsRepository.GetAllAsync();

    /// <summary>
    /// Gets a specific claim by Id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<Claim?> GetClaimAsync(string id) => await _claimsRepository.GetByIdAsync(id);

    /// <summary>
    /// Creates a new claim.
    /// </summary>
    /// <param name="claim"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<Claim> CreateClaimAsync(Claim claim)
    {
        // Validation Rule: ClaimDamageCost cannot exceed 100,000
        if (claim.DamageCost > 100000)
        {
            throw new ArgumentException("Claim damage cost cannot exceed 100,000.");
        }

        // Fetch the related cover to validate the date
        var relatedCover = await _coversRepository.GetByIdAsync(claim.CoverId); 

        if (relatedCover is null)
        {
            throw new ArgumentException($"Related cover with ID {claim.CoverId} does not exist.");
        }

        // Validation Rule: Created date must be within the period of the related Cover
        if (claim.Created < relatedCover.StartDate || claim.Created > relatedCover.EndDate)
        {
            throw new ArgumentException("Claim created date must fall within the related cover's period.");
        }

        claim.Id = Guid.NewGuid().ToString();
        await _claimsRepository.AddAsync(claim);
        await _auditService.AuditClaim(claim.Id, "POST");

        return claim;
    }

    /// <summary>
    /// Deletes an existing claim.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task DeleteClaimAsync(string id)
    {
        await _claimsRepository.DeleteAsync(id);
        await _auditService.AuditClaim(id, "DELETE");
    }   
}