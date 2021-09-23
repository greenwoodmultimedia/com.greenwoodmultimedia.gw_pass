using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gw_pass.Classes
{
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
        /// Fonction qui retourne un tableau contenant les trois chiffres de la version actuelle de gw_pass.
        /// </summary>
        /// <returns>Tableau int[3].</returns>
        public int[] obtenir_numero_version()
        {
            return new int[3] { int.Parse(version.Split(".")[0]), int.Parse(version.Split(".")[0]), int.Parse(version.Split(".")[0]) };
        }
    }
}
