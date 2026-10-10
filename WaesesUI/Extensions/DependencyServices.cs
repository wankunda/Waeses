using Microsoft.Extensions.DependencyInjection;

namespace WaesesUI
{
    public static class DependencyServices
    {
        public static IServiceCollection AddWaesesUIServices(this IServiceCollection services)
            => services.AddWaesesUIServices(null);

        public static IServiceCollection AddWaesesUIServices(this IServiceCollection services, Action<UiOptions>? configure)
        {
            var options = new UiOptions();
            configure?.Invoke(options);
            services.AddSingleton(options);

            services.AddScoped<IToastService, ToastService>();
            services.AddScoped<IMessageBoxService, MessageBoxService>();
            services.AddScoped<IUiDialogService, UiDialogService>();
            services.AddScoped<IUiPopoverService, UiPopoverService>();
            services.AddScoped<IThemeService, ThemeService>();
            services.AddScoped<IUiLocalizationService, UiLocalizationService>();
            return services;
        }
    }
}
