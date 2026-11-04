using Clinic.Application.Common.Interfaces;
using Clinic.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Clinic.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers all application use case handlers, validators, and domain event handlers into the specified service collection.
    /// </summary>
    /// <param name="services">The service collection to populate.</param>
    /// <returns>The updated service collection instance.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {


        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}