namespace gw_pass
{
    /// <summary>
    /// Représente la configuration de gw_pass.
    /// </summary>
    class Configuration
    {
        /// <summary>
        /// Version du programme.
        /// </summary>
        public string version { get; set; }

        /// <summary>
        /// Représente le courriel de l'utilisateur.
        /// </summary>
        public string courriel { get; set; }

        /// <summary>
        /// Représente le mot de passe de l'utilisateur.
        /// </summary>
        public string mot_de_passe { get; set; }

        /// <summary>
        /// Sel ajouté à l'encryption.
        /// </summary>
        public byte[] sel { get; set; }

        /// <summary>
        /// Indique la dernière fois que la clé de décryption a bel et bien été entré.
        /// </summary>
        public string date_initialisation { get; set; }

        /// <summary>
        /// Indique la dernière fois que la clé de décryption a bel et bien été entré.
        /// </summary>
        public string derniere_date_acces { get; set; }

        /// <summary>
        /// Liste des services enregistrés.
        /// </summary>
        public string liste_service { get; set; }
    }
}
