using BoslaPlatform.Application;
using FluentValidation;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        //services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddValidatorsFromAssemblyContaining<ApplicationAssemblyReference>();
        return services;
    }
}
