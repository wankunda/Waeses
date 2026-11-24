using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace WaesesUI.Tests;

public sealed class CommandPaletteTests : TestContext
{
    private static readonly UiCommand[] Commands =
    [
        new() { Label = "Create file", Description = "Add a new file", Group = "File", Execute = () => Task.CompletedTask },
        new() { Label = "Open file", Group = "File", Execute = () => Task.CompletedTask },
        new() { Label = "Preferences", Group = "App", Disabled = true, Execute = () => Task.CompletedTask }
    ];

    public CommandPaletteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddWaesesUIServices();
    }

    private IRenderedComponent<UiCommandPalette> RenderPalette(
        bool isOpen = true,
        Action<bool>? onOpenChanged = null,
        IReadOnlyList<UiCommand>? commands = null) =>
        RenderComponent<UiCommandPalette>(parameters => parameters
            .Add(component => component.Commands, commands ?? Commands)
            .Add(component => component.IsOpen, isOpen)
            .Add(component => component.IsOpenChanged, EventCallback.Factory.Create<bool>(this, value => onOpenChanged?.Invoke(value)))
            .Add(component => component.SearchPlaceholder, "Find an action")
            .Add(component => component.SearchLabel, "Action search"));

    [Fact]
    public void Search_filters_commands_case_insensitively_by_label_description_and_group()
    {
        var cut = RenderPalette();

        cut.Find("input").Input("ADD");
        Assert.Equal("Create file", Assert.Single(cut.FindAll(".ui-command-palette-label")).TextContent.Trim());

        cut.Find("input").Input("app");
        Assert.Equal("Preferences", Assert.Single(cut.FindAll(".ui-command-palette-label")).TextContent.Trim());

        cut.Find("input").Input("missing");
        Assert.Contains("Aucune commande trouvée", cut.Find(".ui-command-palette-empty").TextContent);
    }

    [Fact]
    public void Palette_exposes_dialog_search_and_group_accessibility_markup()
    {
        var cut = RenderPalette();

        Assert.Equal("dialog", cut.Find("[role=dialog]").GetAttribute("role"));
        var titleId = cut.Find("[role=dialog]").GetAttribute("aria-labelledby");
        Assert.Equal(AriaLabelText, cut.Find($"#{titleId}").TextContent);
        Assert.Equal("Action search", cut.Find("input").GetAttribute("aria-label"));
        Assert.Equal("listbox", cut.Find("[role=listbox]").GetAttribute("role"));
        Assert.Contains("File", cut.Find(".ui-command-palette-group").TextContent);
    }

    private const string AriaLabelText = "Palette de commandes";

    [Fact]
    public void Palette_uses_modal_focus_trap_scroll_lock_and_single_escape_handler()
    {
        var cut = RenderPalette();
        var modal = cut.FindComponent<UiModal>();

        Assert.True(modal.Instance.FocusTrap);
        Assert.True(modal.Instance.LockScroll);
        Assert.False(modal.Instance.CloseOnEscape);
        Assert.True(modal.Instance.CloseOnBackdropClick);
        Assert.Contains("ui-command-palette", modal.Instance.Class);
        Assert.Equal("ui-command-palette-backdrop", modal.Instance.BackdropClass);
    }

    [Fact]
    public void Escape_closes_and_notifies_state_change()
    {
        var changes = new List<bool>();
        var cut = RenderPalette(onOpenChanged: changes.Add);

        cut.Find("input").KeyDown("Escape");

        Assert.Empty(cut.FindAll("[role=dialog]"));
        Assert.Equal(new[] { false }, changes);
    }

    [Fact]
    public async Task Global_shortcut_opens_and_notifies_state_change()
    {
        var changes = new List<bool>();
        var cut = RenderPalette(isOpen: false, onOpenChanged: changes.Add);

        await cut.InvokeAsync(() => cut.Instance.OpenFromShortcut());

        Assert.Single(cut.FindAll("[role=dialog]"));
        Assert.Equal(new[] { true }, changes);
    }

    [Fact]
    public async Task Le_service_ouvre_la_palette_avec_les_commandes_fournies()
    {
        var cut = RenderPalette(isOpen: false);
        var commands = new[]
        {
            new UiCommand { Label = "Commande externe", Execute = () => Task.CompletedTask }
        };

        await Services.GetRequiredService<IUiCommandPaletteService>().OpenAsync(commands);

        Assert.Equal("Commande externe", cut.Find(".ui-command-palette-label").TextContent.Trim());
    }

    [Fact]
    public async Task Le_service_signale_quand_aucune_palette_n_est_rendue()
    {
        var service = Services.GetRequiredService<IUiCommandPaletteService>();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.OpenAsync(Commands));

        Assert.Contains("Aucun composant <UiCommandPalette />", error.Message);
    }

    [Fact]
    public void Keyboard_skips_disabled_commands_and_enter_executes_then_closes()
    {
        var executed = new List<string>();
        var commands = new[]
        {
            new UiCommand { Label = "First", Execute = () => { executed.Add("First"); return Task.CompletedTask; } },
            new UiCommand { Label = "Disabled", Disabled = true, Execute = () => { executed.Add("Disabled"); return Task.CompletedTask; } },
            new UiCommand { Label = "Last", Execute = () => { executed.Add("Last"); return Task.CompletedTask; } }
        };
        var changes = new List<bool>();
        var cut = RenderPalette(commands: commands, onOpenChanged: changes.Add);

        cut.Find("input").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Equal("Last", cut.Find("[aria-selected=true] .ui-command-palette-label").TextContent);

        cut.Find("input").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });

        Assert.Equal(new[] { "Last" }, executed);
        Assert.Empty(cut.FindAll("[role=dialog]"));
        Assert.Equal(new[] { false }, changes);
    }

    [Fact]
    public void Disabled_command_is_not_executed_when_clicked()
    {
        var executed = false;
        var commands = new[]
        {
            new UiCommand { Label = "Unavailable", Disabled = true, Execute = () => { executed = true; return Task.CompletedTask; } }
        };
        var cut = RenderPalette(commands: commands);

        cut.Find(".ui-command-palette-option").Click();

        Assert.False(executed);
        Assert.Single(cut.FindAll("[role=dialog]"));
    }

    [Fact]
    public void Global_shortcut_registers_named_interop_with_default_key()
    {
        RenderPalette(isOpen: false);

        var setup = Assert.Single(JSInterop.Invocations, invocation => invocation.Identifier == "UiInterop.setupCommandPalette");
        Assert.Equal("Ctrl+K", setup.Arguments[1]);
    }
}
