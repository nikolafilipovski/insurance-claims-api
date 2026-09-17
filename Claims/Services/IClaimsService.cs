using Claims;

public interface IClaimsService
{
    Task<IEnumerable<Claim>> GetClaimsAsync();

    Task<Claim?> GetClaimAsync(string id);

    Task<Claim> CreateClaimAsync(Claim claim);

    Task DeleteClaimAsync(string id);
}
