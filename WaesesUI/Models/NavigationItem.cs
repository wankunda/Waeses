using Microsoft.AspNetCore.Components.Routing;

namespace WaesesUI
{
    public sealed class NavigationItem
    {
        public string Text { get; init; } = string.Empty;
        public string Href { get; init; } = string.Empty;
        public string? Icon { get; init; }
        public NavLinkMatch Match { get; init; } = NavLinkMatch.Prefix;
        public bool Disabled { get; init; }

        /// <summary>Titre de section : un en-tête s'affiche à chaque changement de groupe dans la liste (les éléments d'un même groupe doivent se suivre).</summary>
        public string? Group { get; init; }
    }
}
