using Microsoft.Extensions.DependencyInjection;

namespace FluentFunctions.Functions.Configuration;

public static class OptionsRegistration
{
    /// <summary>
    /// Binds <typeparamref name="T"/> to a configuration section, validates its data annotations and
    /// does so when the host starts. A missing or invalid setting stops the app at startup with the
    /// key named in the error, instead of surfacing later inside an invocation.
    /// </summary>
    public static IServiceCollection AddValidatedOptions<T>(this IServiceCollection services, string section)
        where T : class
    {
        services.AddOptions<T>().BindConfiguration(section).ValidateDataAnnotations().ValidateOnStart();
        return services;
    }
}
