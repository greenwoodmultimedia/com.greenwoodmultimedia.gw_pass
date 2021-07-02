using Newtonsoft.Json;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Security;

namespace gw_pass
{
    /// <summary>
    /// Classe principale de gw_pass.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Point d'entrée du programme.
        /// </summary>
        /// <param name="args">Arguments du programme</param>
        static void Main(string[] args)
        {
            const string nom_fichier_donnees = "gw_pass_data.json";
            SecureString cle_decryption_utilisateur = null;
            Configuration configuration = null;
            ListeService listeService = null;
            bool en_fonction = true;
            bool authenfifier = false;
            string version = "1.1.0";

            //Changement du titre de la console
            Console.Title = "GW PASS - Votre keychain portatif !";

            en_tete(version);

            //Message de bienvenue
            Console.WriteLine("gw_pass>Bienvenue sur votre keychain portatif !");

            //On va chercher la configuration du fichier de données
            if (File.Exists(nom_fichier_donnees))
            {
                //On va chercher le contenu du fichier de configuration
                string contenu_fichier_donnees = File.ReadAllText(nom_fichier_donnees);

                //On désérialize le json du fichier
                configuration = JsonConvert.DeserializeObject<Configuration>(contenu_fichier_donnees);

                //On affiche un message de succès
                Console.WriteLine("gw_pass>Votre fichier de configuration a été trouvé.");
            }
            //S'il n'existe pas, on va le créer
            else
            {
                ///OBTENTION DU MOT DE PASSE///

                //On n'a pas trouvé le fichier de configuration, alors on va le créer.
                Console.WriteLine("gw_pass>Aucun fichier de configuration n'a été trouvé. Nous allons en créer un avec vous.");

                //On entre la clé de décryption
                Console.Write("gw_pass>Veuillez entrer un mot de passe qui sera utilisé pour l'encryption :");
                cle_decryption_utilisateur = obtenir_mot_de_passe();

                /////////////ENCRYPTION//////////////////

                byte[] sel_random = new byte[8];
                using (RNGCryptoServiceProvider rngCsp = new
    RNGCryptoServiceProvider())
                {
                    // Fill the array with a random value.
                    rngCsp.GetBytes(sel_random);
                }

                listeService = new ListeService
                {
                    services = new System.Collections.Generic.List<Service>{}
                };

                string listeService_json_data = JsonConvert.SerializeObject(listeService, Formatting.Indented);

                string donneesEncrypteListeService = encrypter(listeService_json_data, cle_decryption_utilisateur, sel_random);

                configuration = new Configuration
                {
                    cle_decryption = obtenirHashSha256(cle_decryption_utilisateur),
                    sel = sel_random,
                    derniere_date_acces = DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss"),
                    liste_services = donneesEncrypteListeService
                };

                string configuration_json_data = JsonConvert.SerializeObject(configuration, Formatting.Indented);

                //Succes
                File.WriteAllBytes(@nom_fichier_donnees, Encoding.UTF8.GetBytes(configuration_json_data));
                Console.WriteLine("gw_pass>Fichier de configuration par défaut créer et encrypté !");
            }

            //Authentification
            Console.Write("Veuillez entrer votre clé de décryption :");
            cle_decryption_utilisateur = obtenir_mot_de_passe();

            //Vérification de la clé de décryption afin d'authentifier l'utilisateur
            if (obtenirHashSha256(cle_decryption_utilisateur) == configuration.cle_decryption)
            {
                authenfifier = true;

                string contenu_liste_service = decrypter(configuration.liste_services, cle_decryption_utilisateur, configuration.sel);

                listeService = JsonConvert.DeserializeObject<ListeService>(contenu_liste_service);

                Console.WriteLine("gw_pass>Vous avez utilisé la dernière fois ce fichier le " + configuration.derniere_date_acces + ".");

                //On met à jour la date d'utilisation après avoir afficher la dernière date
                configuration.derniere_date_acces = DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss");

                Console.WriteLine("gw_pass>Vous êtes authentifié !");

                //On entre dans la section commune du programme.
                while (en_fonction && authenfifier)
                {
                    Console.Write("gw_pass>");
                    string commande = Console.ReadLine();
                    if (commande == "quitter")
                    {
                        //On désactive le programme
                        en_fonction = false;

                        string listeService_json_data = JsonConvert.SerializeObject(listeService, Formatting.Indented);

                        string nouveaudonneesEncrypteListeService = encrypter(listeService_json_data, cle_decryption_utilisateur, configuration.sel);

                        configuration.liste_services = nouveaudonneesEncrypteListeService;

                        //On enregistre le data et dans la prochaine boucle, le programme se termine.
                        string configuration_json_data = JsonConvert.SerializeObject(configuration, Formatting.Indented);
                        File.WriteAllBytes(@nom_fichier_donnees, Encoding.UTF8.GetBytes(configuration_json_data));
                    }
                    else if (commande == "liste_service")
                    {
                        if (listeService != null && listeService.services.Count > 0)
                        {
                            Console.WriteLine();
                            Console.WriteLine("Voici la liste des services trouvés : ");
                            Console.WriteLine();
                            for (int i = 0; i < listeService.services.Count; i++)
                            {
                                Console.WriteLine(listeService.services[i].nom);
                            }
                            Console.WriteLine();
                        }
                        else
                        {
                            Console.WriteLine();
                            Console.WriteLine("Aucun service trouvé. Veuillez en ajouter un dans le keychain.");
                            Console.WriteLine();
                        }
                    }
                    else if (commande == "voir_service")
                    {
                        if (listeService != null && configuration.liste_services.Length > 0)
                        {
                            Console.Write("Veuillez entrer le nom du service :");
                            string nom_service = Console.ReadLine();

                            for (int i = 0; i < listeService.services.Count; i++)
                            {
                                if (listeService.services[i].nom == nom_service)
                                {
                                    Console.WriteLine();
                                    Console.WriteLine("Nom du service: " + listeService.services[i].nom);
                                    Console.WriteLine("Mot de passe: " + listeService.services[i].mot_de_passe);
                                    Console.WriteLine();
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine();
                            Console.WriteLine("Aucun service trouvé. Veuillez en ajouter un dans le keychain.");
                            Console.WriteLine();
                        }
                    }
                    else if (commande == "ajouter_service")
                    {
                        Console.Write("Veuillez entrer le nom du service :");
                        string nom_service = Console.ReadLine();
                        Console.Write("Veuillez entrer le mot de passe du service :");
                        string mot_de_passe = Console.ReadLine();

                        Service nouveau_service = new Service()
                        {
                            nom = nom_service,
                            mot_de_passe = mot_de_passe
                        };

                        listeService.services.Add(nouveau_service);
                    }
                    else if (commande == "effacer_console")
                    {
                        Console.Clear();
                    }
                    else if (commande == "derniere_connexion")
                    {
                        Console.WriteLine("Vous avez utilisé la dernière fois ce fichier le " + configuration.derniere_date_acces + ".");
                    }
                    else if (commande == "aide")
                    {
                        Console.WriteLine();
                        Console.WriteLine("Voici les commandes qui sont disponibles");
                        Console.WriteLine();
                        Console.WriteLine("aide | Affiche l'aide que vous voyez présentement.");
                        Console.WriteLine("ajouter_service | Procédure pour ajouter un mot de passe du keychain.");
                        Console.WriteLine("derniere_connexion | Indique la dernière connexion réussie de gw_pass.");
                        Console.WriteLine("effacer_console | Efface la ligne de commande de gw_pass.");
                        Console.WriteLine("liste_service | Procédure pour les services ayant été enregistré dans le keychain.");
                        Console.WriteLine("voir_service | Procédure pour voir un des mots de passe du keychain.");
                        Console.WriteLine("quitter | Ferme gw_pass.");
                        Console.WriteLine();
                    }
                    else
                    {
                        Console.WriteLine();
                        Console.WriteLine("Commande '" + commande + "' non reconnu. Veuillez écrire 'aide' afin d'obtenir la liste des commandes possibles.");
                        Console.WriteLine();
                    }
                }
            }
            //Le programme se ferme dans le cas contraire
            else
            {
                Console.WriteLine("Votre authentification a échoué ! Le programme va se fermer après que vous toucher sur une touche...");
                Console.ReadLine();
            }
        }

        /// <summary>
        /// Affiche l'en-tête du programme.
        /// </summary>
        /// <param name="version">Version du programme.</param>
        public static void en_tete(string version)
        {
            Console.WriteLine();
            Console.WriteLine("-------------------------------");
            Console.WriteLine("--                           --");
            Console.WriteLine("--    GreenWood Multimedia   --");
            Console.WriteLine("--   gw_pass Version " + version + "    --");
            Console.WriteLine("--                           --");
            Console.WriteLine("--          © " + DateTime.Now.ToString("yyyy") + "           --");
            Console.WriteLine("--                           --");
            Console.WriteLine("--    Tous droits réservés   --");
            Console.WriteLine("--                           --");
            Console.WriteLine("-------------------------------");
            Console.WriteLine();
        }

        /// <summary>
        /// Encrypte des données en utilisant l'algorithme AES en mode CBC.
        /// </summary>
        /// <param name="donnees">Données sous format "string" à encrypter.</param>
        /// <param name="cle_encryption">Clé d'encryption de l'utilisateur encrypté par SHA-256.</param>
        /// <param name="sel">Sel de l'utilisateur.</param>
        /// <returns>Retourne les données sous forme encrypté et en format base 64.</returns>
        public static string encrypter(string donnees, SecureString cle_encryption, byte[] sel)
        {
            byte[] clearBytes = Encoding.Unicode.GetBytes(donnees);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(cle_encryption.ToString(), sel, 1000, HashAlgorithmName.SHA256);
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    donnees = Convert.ToBase64String(ms.ToArray());
                }
            }
            return donnees;
        }

        /// <summary>
        /// Décrypte des données en utilisant l'algorithme AES en mode CBC.
        /// </summary>
        /// <param name="donnees_encrypte">Données sous format "string" à décrypter.</param>
        /// <param name="cle_encryption">Clé d'encryption de l'utilisateur encrypté par SHA-256.</param>
        /// <param name="sel">Sel de l'utilisateur.</param>
        /// <returns>Retourne les données décryptées et en format Unicode.</returns>
        public static string decrypter(string donnees_encrypte, SecureString cle_encryption, byte[] sel)
        {
            byte[] cipherBytes = Convert.FromBase64String(donnees_encrypte);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(cle_encryption.ToString(), sel, 1000, HashAlgorithmName.SHA256);
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    donnees_encrypte = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
            return donnees_encrypte;
        }

        /// <summary>
        /// Obtient un mot de passe en ligne de commande sans que personne ne puisse le voir.
        /// </summary>
        /// <returns>Retourne un string sécurisé. Permet de le protéger jusqu'au moment ou il est nécessaire de l'utiliser.</returns>
        public static SecureString obtenir_mot_de_passe()
        {
            // Instantiate the secure string.
            SecureString securePwd = new SecureString();
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);

                // Ignore any key out of range.
                /*if (((int)key.Key) >= 65 && ((int)key.Key <= 90))
                {*/
                    // Append the character to the password.
                    securePwd.AppendChar(key.KeyChar);
                    Console.Write("*");
                /*}*/
                // Exit if Enter key is pressed.
            } while (key.Key != ConsoleKey.Enter);
            Console.WriteLine();

            return securePwd;
        }

        /// <summary>
        /// Hash un mot de passe en sha256.
        /// </summary>
        /// <param name="text">Le mot de passe à hashé sous forme de Secure String.</param>
        /// <returns>Retourne le hash.</returns>
        public static string obtenirHashSha256(SecureString mot_de_passe)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(new System.Net.NetworkCredential(string.Empty, mot_de_passe).Password);
            SHA256Managed hashstring = new SHA256Managed();
            byte[] hash = hashstring.ComputeHash(bytes);
            string hashString = string.Empty;
            foreach (byte x in hash)
            {
                hashString += String.Format("{0:x2}", x);
            }
            return hashString;
        }
    }
}
