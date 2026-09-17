using System.Threading.Channels;

namespace Claims.Auditing;

public class Auditer : IAuditService 
{
    private readonly ChannelWriter<AuditMessage> _channelWriter;

    public Auditer(ChannelWriter<AuditMessage> channelWriter)
    {
        _channelWriter = channelWriter;
    }

    public async Task AuditClaim(string id, string httpRequestType)
    {
        var message = new AuditMessage(id, "Claim", httpRequestType);
        await _channelWriter.WriteAsync(message);
    }

    public async Task AuditCover(string id, string httpRequestType)
    {
        var message = new AuditMessage(id, "Cover", httpRequestType);
        await _channelWriter.WriteAsync(message);
    }
}

public record AuditMessage(string EntityId, string EntityType, string HttpRequestType);