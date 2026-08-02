using Microsoft.EntityFrameworkCore;
using VoteService.Api;

var builder = WebApplication.CreateBuilder(args);

// ---- Services ----

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("VoteDb")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:VoteDb configuration.");

builder.Services.AddDbContext<VoteDbContext>(options => options.UseSqlServer(connectionString));

// Typed HttpClients pointing at the other two services.
// Base addresses come from config so they're different in local dev vs docker-compose
// (e.g. "http://localhost:5001" locally, "http://poll-service:8080" in docker-compose).
var pollServiceUrl = builder.Configuration["Services:PollService"]
    ?? throw new InvalidOperationException("Missing Services:PollService configuration.");
var realtimeServiceUrl = builder.Configuration["Services:RealtimeService"]
    ?? throw new InvalidOperationException("Missing Services:RealtimeService configuration.");

builder.Services.AddHttpClient<PollServiceClient>(client => client.BaseAddress = new Uri(pollServiceUrl));
builder.Services.AddHttpClient<RealtimeServiceClient>(client => client.BaseAddress = new Uri(realtimeServiceUrl));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// Apply any pending EF Core migrations automatically on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VoteDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
