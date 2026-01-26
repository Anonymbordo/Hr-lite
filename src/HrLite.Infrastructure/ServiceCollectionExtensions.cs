using HrLite.Application.Interfaces;
using HrLite.Infrastructure.AI;
using HrLite.Infrastructure.Authentication;
using HrLite.Infrastructure.Persistence;
using HrLite.Infrastructure.Persistence.Repositories;
using HrLite.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HrLite.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<ILeaveTypeRepository, LeaveTypeRepository>();
        services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<LlmRateLimiter>();
        services.AddHttpClient<ILlmClient, OpenAiLlmClient>();

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }

    public static async Task InitializeInfrastructureAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await context.Database.MigrateAsync();
        await DatabaseSeeder.SeedAsync(context);
    }
}
