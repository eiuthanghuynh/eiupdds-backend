using dotenv.net;
using eiupdds_backend.Data;
using eiupdds_backend.Middlewares;
using eiupdds_backend.Services.Implementations;
using eiupdds_backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;

DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog from appsettings.json
builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Configure Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Application Services
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddHttpClient<IRoboflowService, RoboflowService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Global exception handling
app.UseMiddleware<GlobalExceptionMiddleware>();

// Request logging
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Serve static files (uploads)
app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

try
{
    Log.Information("Starting EIU PDDS Web API host...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "EIU PDDS Web API host terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
