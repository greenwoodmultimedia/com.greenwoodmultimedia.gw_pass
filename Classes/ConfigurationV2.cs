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

        /// <summary>
        /// Méthode qui décrypte les noms des services et qui les trient ensuite.
        /// </summary>
        /// <param name="moduleEncryption">Module d'encryption qui permet de décrypter les noms des services.</param>
        public void trier_liste_service(GestionAes moduleEncryption)
        {
            for (int i = 0; i < liste_service.Count; i++)
            {
                liste_service[i].nom = moduleEncryption.decrypter(liste_service[i].nom);
            }
            liste_service.Sort();
            for (int i = 0; i < liste_service.Count; i++)
            {
                liste_service[i].nom = moduleEncryption.encrypter(liste_service[i].nom);
            }
        }
    }
}
