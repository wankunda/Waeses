using Microsoft.JSInterop;

namespace WaesesUI;

public interface IThemeService
{
    bool IsDarkMode { get; }
    event Action? OnThemeChanged;
    Task InitializeThemeAsync();
    Task ToggleThemeAsync();
    Task SetThemeAsync(bool isDark);
}

public class ThemeService : IThemeService
{
    private readonly IJSRuntime _jsRuntime;
    private bool _isDarkMode;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public bool IsDarkMode => _isDarkMode;

    public event Action? OnThemeChanged;

    public async Task InitializeThemeAsync()
    {
        try
        {
            var savedTheme = await _jsRuntime.InvokeAsync<string?>("UiInterop.getStoredTheme");
            if (!string.IsNullOrEmpty(savedTheme))
            {
                _isDarkMode = savedTheme == "dark";
            }
            else
            {
                // Default to system preference
                _isDarkMode = await _jsRuntime.InvokeAsync<bool>("UiInterop.prefersDark");
            }
            await ApplyThemeAsync();
        }
        catch
        {
            // Fallback if JS runtime is not available yet (e.g. prerendering)
            _isDarkMode = false;
        }
    }

    public async Task ToggleThemeAsync()
    {
        _isDarkMode = !_isDarkMode;
        await ApplyThemeAsync();
        OnThemeChanged?.Invoke();
    }

    public async Task SetThemeAsync(bool isDark)
    {
        if (_isDarkMode != isDark)
        {
            _isDarkMode = isDark;
            await ApplyThemeAsync();
            OnThemeChanged?.Invoke();
        }
    }

    private async Task ApplyThemeAsync()
    {
        try
        {
            var themeName = _isDarkMode ? "dark" : "light";
            await _jsRuntime.InvokeVoidAsync("UiInterop.setTheme", themeName);
        }
        catch
        {
            // Ignored during prerender
        }
    }
}
