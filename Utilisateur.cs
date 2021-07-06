using System.Net;
using System.Security;

namespace gw_pass
{
    class Utilisateur
    {
        /// <summary>
        /// Représente le courriel de l'utilisateur.
        /// </summary>
        public NetworkCredential credentiels { set; get; }
    }
}
