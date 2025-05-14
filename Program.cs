using Microsoft.AspNetCore.SignalR;
using SignalR.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR(); // ?? SignalR agregado
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 200 * 1024 * 1024; // 200 MB para archivos grandes
});

// Aquí registrás el CustomUserIdProvider
builder.Services.AddSingleton<IUserIdProvider, SignalR.Hubs.CustomUserIdProvider>();
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
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

// ? Agrega esta línea para que el Hub esté disponible
app.MapHub<SignalR.Hubs.ChatHub>("/chathub");

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
