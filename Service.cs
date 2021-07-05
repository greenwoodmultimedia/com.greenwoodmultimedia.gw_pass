﻿using System;

namespace gw_pass
{
    /// <summary>
    /// Représente un service.
    /// </summary>
    class Service : IEquatable<Service>, IComparable<Service>
    {
        /// <summary>
        /// Représente le nom d'un service.
        /// </summary>
        public string nom { set; get; }

        /// <summary>
        /// Représente l'identifiant d'un service.
        /// </summary>
        public string identifiant { set; get; }

        /// <summary>
        /// Représente le mot de passe d'un service.
        /// </summary>
        public string mot_de_passe { set; get; }

        public void afficher_service()
        {
            Console.WriteLine();
            Console.WriteLine("Nom du service: " + nom);
            Console.WriteLine("Identifiant du service: " + identifiant);
            Console.WriteLine("Mot de passe: " + mot_de_passe);
            Console.WriteLine();
        }

        int IComparable<Service>.CompareTo(Service? service)
        {
            if (service == null)
                return 1;
            else
                return this.nom.CompareTo(service.nom);
        }

        bool IEquatable<Service>.Equals(Service? other)
        {
            if (other == null) return false;
            return (this.nom.Equals(other.nom));
        }
    }
}
