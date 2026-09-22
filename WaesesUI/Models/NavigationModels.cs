using Microsoft.AspNetCore.Components.Routing;

namespace WaesesUI;

public sealed record UiBreadcrumbItem(string Text, string? Href = null, NavLinkMatch Match = NavLinkMatch.Prefix);

public sealed record UiStepItem(string Title, string? Description = null, bool Completed = false, bool Disabled = false);

public sealed record UiTimelineItem(string Title, string? Description = null, string? Date = null, UiColor Color = UiColor.Primary);
