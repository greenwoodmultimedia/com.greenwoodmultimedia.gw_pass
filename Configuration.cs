namespace gw_pass
{
    /// <summary>
    /// Représente la configuration de gw_pass.
    /// </summary>
    class Configuration
    {
        /// <summary>
        /// Clé de décryption.
        /// </summary>
        public string cle_decryption { get; set; }

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
        public string liste_services { get; set; }
    }
}
