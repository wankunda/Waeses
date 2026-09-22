using Microsoft.Extensions.DependencyInjection;

namespace WaesesUI
{
    public static class DependencyServices
    {
        public static IServiceCollection AddWaesesUIServices(this IServiceCollection services)
        {
            services.AddScoped<IToastService, ToastService>();
            services.AddScoped<IMessageBoxService, MessageBoxService>();
            services.AddScoped<IThemeService, ThemeService>();
            services.AddScoped<IUiLocalizationService, UiLocalizationService>();
            return services;
        }
    }
}
