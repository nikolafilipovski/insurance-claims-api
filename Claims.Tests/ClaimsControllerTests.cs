using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using Xunit;

namespace Claims.Tests
{
    public class ClaimsControllerTests
    {
        [Fact]
        public async Task Get_Claims()
        {
            //TODO: Apart from ensuring 200 OK being returned, what else can be asserted?
            var application = new WebApplicationFactory<Program>().WithWebHostBuilder(_ => { });
            var client = application.CreateClient();

            var response = await client.GetAsync("/Claims");

            response.EnsureSuccessStatusCode();

            // Assert the content type 
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());

            // Deserialize and assert the payload is a valid IEnumerable<Claim>
            var claims = await response.Content.ReadFromJsonAsync<IEnumerable<Claim>>();
            Assert.NotNull(claims);
        }

    }
}
