using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Nạp ocelot.json (mặc định) rồi merge với ocelot.{Environment}.json nếu có
// (ví dụ ocelot.Docker.json khi ASPNETCORE_ENVIRONMENT=Docker) - đúng theo
// cách Ocelot khuyến nghị để đổi Host/Port giữa local dev và docker-compose
// mà không phải sửa code.
builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

builder.Services.AddOcelot(builder.Configuration);
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Bắt buộc cho route SignalR/WebSocket phía trên (DownstreamScheme: "ws").
app.UseWebSockets();

// UseOcelot() là async, phải chạy sau MapControllers (Ocelot chỉ bắt các request
// không khớp route nào của app) và trước app.Run() - đây là pattern chuẩn theo
// tài liệu Ocelot, không giống middleware thông thường.
await app.UseOcelot();

app.Run();
