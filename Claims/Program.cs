using Claims;
using Claims.Auditing;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Testcontainers.MongoDb;
using Testcontainers.MsSql;

var builder = WebApplication.CreateBuilder(args);

// Start Testcontainers for SQL Server and MongoDB
var sqlContainer = (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
        ? new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        : new()

    ).Build();

var mongoContainer = new MongoDbBuilder()
    .WithImage("mongo:latest")
    .Build();

await sqlContainer.StartAsync();
await mongoContainer.StartAsync();

// Add services to the container.
builder.Services
    .AddControllers()
    .AddJsonOptions(x =>
    {
        x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddDbContext<AuditContext>(options =>
    options.UseSqlServer(sqlContainer.GetConnectionString()));

builder.Services.AddDbContext<ClaimsContext>(options =>
{
    var client = new MongoClient(mongoContainer.GetConnectionString());
    var database = client.GetDatabase(builder.Configuration["MongoDb:DatabaseName"]); // Use a default/test database name
    options.UseMongoDB(database.Client, database.DatabaseNamespace.DatabaseName);
});

// Services
builder.Services.AddScoped<IClaimsService, ClaimsService>();
builder.Services.AddScoped<ICoversService, CoversService>();

// Create an unbounded channel
var auditChannel = Channel.CreateUnbounded<AuditMessage>();

// Register the Producer and Consumer halves of the channel
builder.Services.AddSingleton(auditChannel.Writer);
builder.Services.AddSingleton(auditChannel.Reader);

// Register the Background Worker to run continuously
builder.Services.AddHostedService<AuditBackgroundWorker>();

// Register the Auditer Service
builder.Services.AddScoped<IAuditService, Auditer>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AuditContext>();
    context.Database.Migrate();
}

app.Run();

public partial class Program { }
