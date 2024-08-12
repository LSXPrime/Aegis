using System.Text.Json;
using Aegis.Server.Data;
using Aegis.Server.DTOs;
using Aegis.Server.Filters;
using Aegis.Server.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Aegis.Server;

public class Startup(IConfiguration configuration)
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 1. Configure Logging
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        // 2. Configure Database
        services.AddControllers();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

        // 3. Configure Filters
        services.AddMvc(options => { options.Filters.Add<ApiExceptionFilter>(); });

        // 4. Configure Settings
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        // 5. Register Services
        services.AddScoped<AuthService>();
        services.AddScoped<LicenseService>();
        services.AddHostedService<HeartbeatMonitor>();
        services.AddMemoryCache();

        // 6. Configure Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection()
            .UseSerilogRequestLogging()
            .UseRouting()
            .UseAuthentication()
            .UseAuthorization()
            .UseEndpoints(endpoints => { endpoints.MapControllers(); })
            .UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";

                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (contextFeature != null)
                    {
                        var error = new { message = contextFeature.Error.Message };
                        await context.Response.WriteAsync(JsonSerializer.Serialize(error));

                        // Logging
                        Log.Error(contextFeature.Error, "An unhandled exception occurred.");
                        Console.WriteLine($"Stack Trace: {contextFeature.Error.StackTrace}");
                    }
                });
            });
    }
}