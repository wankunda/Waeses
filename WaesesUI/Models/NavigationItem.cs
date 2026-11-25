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

        /// <summary>Nom du groupe : les liens d'un même groupe s'affichent en retrait sous son titre (ils doivent se suivre dans la liste). Voir <see cref="NavigationGroup"/> pour le rendre repliable.</summary>
        public string? Group { get; init; }
    }
}
