namespace WaesesUI;

public sealed record UiDateRange(DateTime? Start, DateTime? End);

public sealed record UiCarouselItem(string Src, string Alt, string? Caption = null);
