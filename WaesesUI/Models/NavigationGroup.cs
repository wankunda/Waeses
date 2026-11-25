namespace WaesesUI
{
    /// <summary>
    /// Réglages d'un groupe de <see cref="UiNavMenu"/>. Le groupe est retrouvé par son nom :
    /// <see cref="Name"/> doit être égal à la propriété <see cref="NavigationItem.Group"/> de ses liens.
    /// </summary>
    /// <param name="Name">Nom du groupe, identique à <see cref="NavigationItem.Group"/>.</param>
    public sealed record NavigationGroup(string Name)
    {
        /// <summary>
        /// Groupe repliable (vrai) ou fixe (faux). Laissé à <c>null</c>, le groupe suit
        /// le paramètre <c>CollapsibleGroups</c> du menu.
        /// </summary>
        public bool? Collapsible { get; init; }

        /// <summary>Groupe replié au premier affichage (sans effet sur un groupe fixe ou sur celui de la page active).</summary>
        public bool InitiallyCollapsed { get; init; }
    }
}
