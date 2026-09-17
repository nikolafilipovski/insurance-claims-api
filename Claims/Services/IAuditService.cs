public interface IAuditService
{
    Task AuditClaim(string id, string httpRequestType);
    Task AuditCover(string id, string httpRequestType);
}