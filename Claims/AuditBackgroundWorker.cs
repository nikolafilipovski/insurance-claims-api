using System.Threading.Channels;

namespace Claims.Auditing;

public class AuditBackgroundWorker : BackgroundService
{
    private readonly ChannelReader<AuditMessage> _channelReader;
    private readonly IServiceProvider _serviceProvider;

    public AuditBackgroundWorker(ChannelReader<AuditMessage> channelReader, IServiceProvider serviceProvider)
    {
        _channelReader = channelReader;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Continuously wait for and read messages as they arrive
        await foreach (var message in _channelReader.ReadAllAsync(stoppingToken))
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AuditContext>();

            if (message.EntityType == "Claim")
            {
                dbContext.ClaimAudits.Add(new ClaimAudit
                {
                    ClaimId = message.EntityId,
                    HttpRequestType = message.HttpRequestType,
                    Created = DateTime.UtcNow
                });
            }
            else if (message.EntityType == "Cover")
            {
                dbContext.CoverAudits.Add(new CoverAudit
                {
                    CoverId = message.EntityId,
                    HttpRequestType = message.HttpRequestType,
                    Created = DateTime.UtcNow
                });
            }

            // Execute the DB insert in the background
            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
}