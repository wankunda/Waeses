namespace WaesesUIDocs.Examples;

/// <summary>Images de démonstration générées en SVG intégré (data URI) : les exemples n'effectuent aucune requête réseau.</summary>
public static class ExampleImages
{
    public static string Placeholder(string color, string label, int width = 800, int height = 450) =>
        "data:image/svg+xml;utf8," + Uri.EscapeDataString(
            $"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}' viewBox='0 0 {width} {height}'>" +
            $"<rect width='100%' height='100%' fill='{color}'/>" +
            $"<text x='50%' y='50%' fill='white' font-size='{height / 9}' font-family='sans-serif' text-anchor='middle' dominant-baseline='middle'>{label}</text></svg>");
}
