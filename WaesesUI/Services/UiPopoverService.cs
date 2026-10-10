using Microsoft.AspNetCore.Components;

namespace WaesesUI;

/// <summary>Position préférée du popover par rapport à son ancre. Inversée automatiquement s'il manque de la place.</summary>
public enum UiPopoverPlacement
{
    BottomStart,
    Bottom,
    BottomEnd,
    TopStart,
    Top,
    TopEnd,
    Left,
    Right
}

/// <summary>Popover enregistré auprès du <see cref="UiPopoverProvider"/>.</summary>
public sealed class UiPopoverEntry
{
    public Guid Id { get; } = Guid.NewGuid();
    public string LayerId => $"ui-popover-{Id:N}";
    public string AnchorId => $"ui-popover-anchor-{Id:N}";

    public bool Open { get; set; }
    public RenderFragment? Content { get; set; }
    public UiPopoverPlacement Placement { get; set; } = UiPopoverPlacement.BottomStart;
    public int Offset { get; set; } = 8;
    public bool MatchAnchorWidth { get; set; }
    public bool CloseOnOutsideClick { get; set; } = true;
    public bool CloseOnEscape { get; set; } = true;
    public string? AnchorSelector { get; set; }
    public string? Class { get; set; }
    public string? Style { get; set; }

    /// <summary>Appelé par le provider quand l'utilisateur demande la fermeture (clic extérieur, Échap).</summary>
    public Func<Task>? RequestClose { get; set; }
}

public interface IUiPopoverService
{
    /// <summary>Vrai tant qu'un <see cref="UiPopoverProvider"/> est rendu.</summary>
    bool HasProvider { get; }

    /// <summary>Levé quand la liste ou le contenu d'un popover change.</summary>
    event Action? Changed;

    IReadOnlyList<UiPopoverEntry> Entries { get; }

    void Register(UiPopoverEntry entry);
    void Remove(UiPopoverEntry entry);

    /// <summary>Signale qu'une entrée a été modifiée (ouverture, contenu, placement).</summary>
    void Update();

    void AttachProvider();
    void DetachProvider();
}

public class UiPopoverService : IUiPopoverService
{
    private readonly List<UiPopoverEntry> _entries = new();
    private int _providers;

    public bool HasProvider => _providers > 0;
    public event Action? Changed;
    public IReadOnlyList<UiPopoverEntry> Entries => _entries;

    public void Register(UiPopoverEntry entry)
    {
        if (_entries.Contains(entry)) return;
        _entries.Add(entry);
        Changed?.Invoke();
    }

    public void Remove(UiPopoverEntry entry)
    {
        if (_entries.Remove(entry)) Changed?.Invoke();
    }

    public void Update() => Changed?.Invoke();

    public void AttachProvider()
    {
        _providers++;
        Changed?.Invoke();
    }

    public void DetachProvider()
    {
        _providers = Math.Max(0, _providers - 1);
        Changed?.Invoke();
    }
}
