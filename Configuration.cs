namespace gw_pass
{
    /// <summary>
    /// Représente la configuration de gw_pass.
    /// </summary>
    class Configuration
    {
        /// <summary>
        /// Représente l'utilisateur qui s'est inscrit au départ de l'application.
        /// </summary>
        public Utilisateur utilisateur { get; set; }

        /// <summary>
        /// Sel ajouté à l'encryption.
        /// </summary>
        public byte[] sel { get; set; }

        /// <summary>
        /// Indique la dernière fois que la clé de décryption a bel et bien été entré.
        /// </summary>
        public string derniere_date_acces { get; set; }

        /// <summary>
        /// Liste des services enregistrés.
        /// </summary>
        public ListeService liste_service { get; set; }
    }
}
