using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.FileProviders;
using SignalR.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// SignalR
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 200 * 1024 * 1024; // 200 MB
});

// Aumentar límite de tamaño para archivos grandes
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 200 * 1024 * 1024; // 200 MB
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 200 * 1024 * 1024; // 200 MB
});

// CORS: Permite cualquier origen (útil para desarrollo)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed(_ => true);
    });
});

// CustomUserIdProvider
builder.Services.AddSingleton<IUserIdProvider, SignalR.Hubs.CustomUserIdProvider>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.MapHub<SignalR.Hubs.ChatHub>("/chathub");

app.UseStaticFiles();

// Para la carpeta "uploads":
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "uploads")),
    RequestPath = "/uploads"
});

app.Run();

 
//using Microsoft.AspNetCore.SignalR;
//using SignalR.Hubs;
//
//var builder = WebApplication.CreateBuilder(args);
//
//// Servicios
//builder.Services.AddControllers();
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
//
//builder.Services.AddSignalR(options =>
//{
//    options.MaximumReceiveMessageSize = 200 * 1024 * 1024; // 200MB
//});
//
//// Habilita CORS
//builder.Services.AddCors(options =>
//{
//    options.AddDefaultPolicy(policy =>
//    {
//        policy.AllowAnyHeader()
//              .AllowAnyMethod()
//              .AllowCredentials()
//              .SetIsOriginAllowed(_ => true);
//    });
//});
//
//// Custom User ID
//builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
//
//var app = builder.Build();
//
//// Middlewares
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}
//
//app.UseHttpsRedirection();
//
//app.UseCors(); // <-- importante
//
//app.UseAuthorization();
//
//app.MapControllers();
//app.MapHub<ChatHub>("/chathub"); // <-- endpoint para SignalR
//app.Run("http://0.0.0.0:10000");
//
