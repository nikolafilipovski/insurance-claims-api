using System.Threading.Channels;
using Claims.Auditing;
using Xunit;

namespace Claims.Tests;

public class AuditerTests
{
    [Fact]
    public async Task AuditClaim_WritesMessageToChannel()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<AuditMessage>();
        var auditer = new Auditer(channel.Writer);

        // Act
        await auditer.AuditClaim("123", "POST");

        // Assert
        var message = await channel.Reader.ReadAsync();
        Assert.Equal("123", message.EntityId);
        Assert.Equal("Claim", message.EntityType);
        Assert.Equal("POST", message.HttpRequestType);
    }

    [Fact]
    public async Task AuditCover_WritesMessageToChannel()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<AuditMessage>();
        var auditer = new Auditer(channel.Writer);

        // Act
        await auditer.AuditCover("456", "DELETE");

        // Assert
        var message = await channel.Reader.ReadAsync();
        Assert.Equal("456", message.EntityId);
        Assert.Equal("Cover", message.EntityType);
        Assert.Equal("DELETE", message.HttpRequestType);
    }
}