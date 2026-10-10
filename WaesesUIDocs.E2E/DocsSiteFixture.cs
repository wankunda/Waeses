using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace WaesesUIDocs.E2E;

/// <summary>
/// Démarre le site de documentation <b>tel qu'il est publié</b> (Release, trimmé) derrière un serveur statique local avec repli
/// sur <c>index.html</c> (comme GitHub Pages avec son <c>404.html</c>), puis lance Chromium.
/// Partagé par tous les tests : la publication (~1 min) n'a lieu qu'une fois.
/// </summary>
public sealed class DocsSiteFixture : IAsyncLifetime
{
    private WebApplication? _server;
    private IPlaywright? _playwright;
    private string? _publishDirectory;
    private bool _ownsPublishDirectory;

    public string BaseUrl { get; private set; } = string.Empty;
    public IBrowser Browser { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        // DOCS_E2E_SITE : dossier wwwroot déjà publié (accélère les essais locaux et permet de tester l'artefact d'une CI).
        var existing = Environment.GetEnvironmentVariable("DOCS_E2E_SITE");
        if (!string.IsNullOrEmpty(existing))
        {
            _publishDirectory = existing;
        }
        else
        {
            _publishDirectory = Path.Combine(Path.GetTempPath(), "waesesui-docs-e2e-" + Guid.NewGuid().ToString("N")[..8]);
            _ownsPublishDirectory = true;
            await PublishAsync(_publishDirectory);
            _publishDirectory = Path.Combine(_publishDirectory, "wwwroot");
        }

        await StartServerAsync(_publishDirectory);
        await StartBrowserAsync();
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null) await Browser.CloseAsync();
        _playwright?.Dispose();
        if (_server is not null) await _server.DisposeAsync();

        if (_ownsPublishDirectory && _publishDirectory is not null)
        {
            try { Directory.Delete(Path.GetDirectoryName(_publishDirectory)!, recursive: true); }
            catch (IOException) { /* fichiers encore verrouillés : sans conséquence */ }
        }
    }

    // ---------- Publication ----------
    private static async Task PublishAsync(string output)
    {
        var project = Path.Combine(FindRepositoryRoot(), "WaesesUIDocs", "WaesesUIDocs.csproj");
        var start = new ProcessStartInfo("dotnet", $"publish \"{project}\" --configuration Release --output \"{output}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(start)!;
        var log = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"La publication du site a échoué ({process.ExitCode}).\n{await log}\n{await error}");
        }
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Waeses.slnx"))) return directory.FullName;
        }
        throw new InvalidOperationException("Waeses.slnx introuvable depuis " + AppContext.BaseDirectory);
    }

    // ---------- Serveur statique ----------
    private async Task StartServerAsync(string wwwroot)
    {
        var provider = new FileExtensionContentTypeProvider();
        provider.Mappings[".dat"] = "application/octet-stream";
        provider.Mappings[".blat"] = "application/octet-stream";
        provider.Mappings[".webcil"] = "application/octet-stream";

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();

        var files = new PhysicalFileProvider(wwwroot);
        app.UseStaticFiles(new StaticFileOptions { FileProvider = files, ContentTypeProvider = provider, ServeUnknownFileTypes = true });
        // Repli SPA : toute adresse inconnue (lien profond) renvoie l'application, comme le 404.html de GitHub Pages.
        app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = files });

        await app.StartAsync();
        BaseUrl = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First().TrimEnd('/');
        _server = app;
    }

    // ---------- Navigateur ----------
    private async Task StartBrowserAsync()
    {
        _playwright = await Playwright.CreateAsync();
        try
        {
            Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        }
        catch (PlaywrightException)
        {
            // Premier lancement sur la machine : télécharge Chromium (une seule fois), puis réessaie.
            var exitCode = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
            if (exitCode != 0) throw new InvalidOperationException("Impossible d'installer Chromium pour Playwright (code " + exitCode + ").");
            Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        }
    }
}

[CollectionDefinition(Name)]
public sealed class DocsSiteCollection : ICollectionFixture<DocsSiteFixture>
{
    public const string Name = "Site de documentation";
}
