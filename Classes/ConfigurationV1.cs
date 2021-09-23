using gw_pass.Classes;

namespace gw_pass
{
    /// <summary>
    /// Représente la configuration de gw_pass.
    /// </summary>
    class ConfigurationV1 : Configuration
    {
        /// <summary>
        /// Liste des services enregistrés.
        /// </summary>
        public string liste_service { get; set; }
    }
}
