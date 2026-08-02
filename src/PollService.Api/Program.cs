using Microsoft.EntityFrameworkCore;
using PollService.Api;

var builder = WebApplication.CreateBuilder(args);

// ---- Services ----

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("PollDb")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:PollDb configuration.");

builder.Services.AddDbContext<PollDbContext>(options => options.UseSqlServer(connectionString));

// Frontend (running on a different port/origin during development) needs to call this API.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// Apply any pending EF Core migrations automatically on startup.
// Trước đây dùng EnsureCreated() cho nhanh - giờ chuyển sang Migrate() vì đã có
// migration history (dotnet ef migrations add), đúng quy trình chuẩn.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PollDbContext>();
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
