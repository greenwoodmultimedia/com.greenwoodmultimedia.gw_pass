using gw_pass.Classes;
using System.Collections.Generic;

namespace gw_pass
{
    /// <summary>
    /// Représente la configuration de gw_pass.
    /// </summary>
    class ConfigurationV2 : Configuration
    {
        /// <summary>
        /// Liste des services enregistrés.
        /// </summary>
        public List<Service> liste_service { get; set; }
    }
}
