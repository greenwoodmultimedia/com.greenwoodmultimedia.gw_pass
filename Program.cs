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
            //Constantes
            const string nom_fichier_donnees = "./data/gw_pass_data.json";

            //Variables du programme
            SecureString cle_decryption_utilisateur = null;
            Configuration configuration = null;
            ListeService listeService = null;
            bool en_fonction = true;
            bool authentifier = false;
            string version = "1.4.0";

            //Changement du titre de la console
            Console.Title = "GW PASS - Votre keychain portatif !";

            en_tete(version);

            //Message de bienvenue
            Console.WriteLine("Bienvenue sur votre keychain portatif !");

            //On va chercher la configuration du fichier de données
            if (File.Exists(nom_fichier_donnees))
            {
                //On va chercher le contenu du fichier de configuration
                string contenu_fichier_donnees = File.ReadAllText(nom_fichier_donnees);

                //On désérialize le json du fichier
                configuration = JsonConvert.DeserializeObject<Configuration>(contenu_fichier_donnees);

                //On affiche un message de succès
                Console.WriteLine("Votre fichier de configuration a été trouvé.");
            }
            //S'il n'existe pas, on va le créer
            else
            {
                ///OBTENTION DU MOT DE PASSE///

                //On n'a pas trouvé le fichier de configuration, alors on va le créer.
                Console.WriteLine("Aucun fichier de configuration n'a été trouvé. Nous allons en créer un avec vous.");

                //On entre la clé de décryption par l'utilisateur
                Console.WriteLine();
                Console.Write("Veuillez entrer un mot de passe qui sera utilisé pour l'encryption :");
                cle_decryption_utilisateur = obtenir_mot_de_passe();
                Console.WriteLine();

                /////////////ENCRYPTION//////////////////

                //Création du sel unique à chaque création de fichier encrypté
                byte[] sel_random = new byte[8];
                using (RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider())
                {
                    //Rempli le tableau de byte null
                    rngCsp.GetBytes(sel_random);
                }

                //Création de l'objet représentant la liste des services
                listeService = new ListeService
                {
                    services = new System.Collections.Generic.List<Service>{}
                };

                //Conversion de l'objet des services c# en json
                string listeService_json_data = JsonConvert.SerializeObject(listeService, Formatting.Indented);

                //On encrypte les données concernant les services.
                string donneesEncrypteListeService = encrypter(listeService_json_data, cle_decryption_utilisateur, sel_random);

                //Création de l'objet qui représentera la configuration
                configuration = new Configuration
                {
                    cle_decryption = obtenirHashSha256(cle_decryption_utilisateur),
                    sel = sel_random,
                    derniere_date_acces = DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss"),
                    liste_services = donneesEncrypteListeService
                };

                /////////////SAUVEGARDE//////////////////

                //Création du dossier qui contiendra les données de l'application s'il n'existe pas.
                if (!Directory.Exists("./data"))
                {
                    Directory.CreateDirectory("data");
                }

                //Sauvegarde des données de l'application
                bool succes = sauvegarder_donnees(configuration, nom_fichier_donnees);

                //Si une erreur survient lors du write du fichier de config, on doit stopper l'éxécution. 
                if(!succes)
                {
                    Console.WriteLine();
                    Console.WriteLine("gw_pass>Une erreur est survenue lors de la tentative d'écriture du fichier de configuration.");
                    Console.WriteLine();

                    //On sort du programme, car il n'a rien à faire.
                    return;
                }

                //Succes !
                Console.WriteLine("gw_pass>Fichier de configuration par défaut créer et encrypté !");
            }

            /////////////AUTHENFICATION//////////////////

            //On redemande la clé de décryption afin d'authentifier l'utilisateur
            Console.WriteLine();
            Console.Write("Veuillez entrer votre clé de décryption :");
            cle_decryption_utilisateur = obtenir_mot_de_passe();
            Console.WriteLine();

            //Vérification de la clé de décryption afin d'authentifier l'utilisateur
            if (obtenirHashSha256(cle_decryption_utilisateur) == configuration.cle_decryption)
            {
                //On flag l'utilisateur comme authentifier
                authentifier = true;

                //On décrypte la liste des services qui sera en format json
                string contenu_liste_service = decrypter(configuration.liste_services, cle_decryption_utilisateur, configuration.sel);

                //On convertit le format json en un objet c#
                listeService = JsonConvert.DeserializeObject<ListeService>(contenu_liste_service);

                //Message destiné à l'utilisateur à sa connexion
                Console.WriteLine("Vous êtes authentifié !");
                Console.WriteLine("Vous avez utilisé la dernière fois ce fichier le " + configuration.derniere_date_acces + ".");
                Console.WriteLine();

                //On met à jour la date d'utilisation après avoir afficher la dernière date
                configuration.derniere_date_acces = DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss");

                //On entre dans la section commune du programme.
                while (en_fonction && authentifier)
                {
                    //Invite de commande de gw_pass
                    Console.Write("gw_pass>");

                    //Lecture de la commande
                    string commande = Console.ReadLine();

                    //Permet de quitter le programme.
                    if (commande == "quitter")
                    {
                        //On désactive le programme
                        en_fonction = false;

                        string listeService_json_data = JsonConvert.SerializeObject(listeService, Formatting.Indented);

                        string nouveaudonneesEncrypteListeService = encrypter(listeService_json_data, cle_decryption_utilisateur, configuration.sel);

                        configuration.liste_services = nouveaudonneesEncrypteListeService;

                        //On enregistre le data et dans la prochaine boucle, le programme se termine.
                        sauvegarder_donnees(configuration, nom_fichier_donnees);
                    }
                    //Affiche la liste des services.
                    else if (commande == "liste_service")
                    {
                        if (listeService != null && listeService.services.Count > 0)
                        {
                            Console.WriteLine();
                            Console.WriteLine("Voici la liste des services trouvés : ");
                            Console.WriteLine();
                            for (int i = 0; i < listeService.services.Count; i++)
                            {
                                Console.WriteLine("- " + listeService.services[i].nom);
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
                    //Permet de voir un service en particulier
                    else if (commande == "voir_service")
                    {
                        if (listeService != null && listeService.services.Count > 0)
                        {
                            bool trouve = false;

                            Console.WriteLine();
                            Console.Write("Veuillez entrer le nom du service: ");
                            string nom_service = Console.ReadLine();
                            Console.WriteLine();

                            for (int i = 0; i < listeService.services.Count; i++)
                            {
                                if (listeService.services[i].nom == nom_service)
                                {
                                    //On affiche une en-tête aux informations du service
                                    Console.WriteLine();
                                    Console.WriteLine("INFORMATIONS DU SERVICE");

                                    //Affichage des données via l'appel de afficher_service
                                    listeService.services[i].afficher_service();

                                    //On indique qu'on a trouvé le service
                                    trouve = true;
                                }
                            }

                            if(trouve == false)
                            {
                                Console.WriteLine("Ce service n'existe pas !");
                                Console.WriteLine();
                            }
                        }
                        else
                        {
                            Console.WriteLine();
                            Console.WriteLine("Aucun service trouvé. Veuillez en ajouter un dans le keychain.");
                            Console.WriteLine();
                        }
                    }
                    //Permet d'ajouter un service en particulier
                    else if (commande == "ajouter_service")
                    {
                        bool trouve = false;

                        //On demande le nom du nouveau service
                        Console.WriteLine();
                        Console.Write("Veuillez entrer le nom du service: ");
                        string nom_service = Console.ReadLine();

                        //On va chercher pour voir si le service existe déjà
                        for (int i = 0; i < listeService.services.Count; i++)
                        {
                            if (listeService.services[i].nom == nom_service)
                            {
                                trouve = true;
                            }
                        }

                        //Si le service existe déjà, on afficher une erreur
                        if (trouve)
                        {
                            Console.WriteLine();
                            Console.WriteLine("Un service du même nom existe déjà ! Le service demandé ne sera pas ajouté !");
                            Console.WriteLine();
                            continue;
                        }


                        //Sinon, on continue et on demande les informations du service
                        Console.Write("Veuillez entrer le courriel du service: ");
                        string identifiant = Console.ReadLine();

                        Console.Write("Veuillez entrer le mot de passe du service: ");
                        string mot_de_passe = Console.ReadLine();

                        //Création de l'objet Service
                        Service nouveau_service = new Service()
                        {
                            nom = nom_service,
                            identifiant = identifiant,
                            mot_de_passe = mot_de_passe
                        };

                        //Ajout de l'objet à la liste de service
                        listeService.services.Add(nouveau_service);

                        //Affichage du succès de l'opération
                        Console.WriteLine();
                        Console.Write("Le service a été ajouté avec succès !");
                        Console.WriteLine();
                        Console.WriteLine();
                    }
                    //Permet de changer d'un service
                    else if(commande == "changer_service")
                    {
                        bool trouve = false;

                        //On va demander le service à trouver
                        Console.WriteLine();
                        Console.Write("Veuillez entrer le nom du service à modifier: ");
                        string nom_service = Console.ReadLine();

                        //On va tenter de trouver le service
                        for (int i = 0; i < listeService.services.Count; i++)
                        {
                            if (listeService.services[i].nom == nom_service)
                            {
                                trouve = true;

                                //Affichage des anciennes informations du service
                                Console.WriteLine();
                                Console.WriteLine("INFORMATIONS DU SERVICE AVANT MODIFICATIONS");

                                //Affichage des données via l'appel de afficher_service
                                listeService.services[i].afficher_service();

                                //Obtention des nouvelles données
                                Console.WriteLine();
                                Console.Write("Veuillez entrer le nouveau nom du service :");
                                string nouveau_nom_service = Console.ReadLine();
                                Console.Write("Veuillez entrer le nouvel identifiant du service :");
                                string nouveau_identifiant_service = Console.ReadLine();
                                Console.Write("Veuillez entrer le nouveau mot de passe du service :");
                                string nouveau_mot_de_passe = Console.ReadLine();

                                //On modifie l'objet de la liste des services
                                listeService.services[i].nom = nouveau_nom_service;
                                listeService.services[i].identifiant = nouveau_identifiant_service;
                                listeService.services[i].mot_de_passe = nouveau_mot_de_passe;

                                //Si tout à réussi, un message de succès s'affiche
                                Console.WriteLine();
                                Console.Write("Le service a été modifié !");
                                Console.WriteLine();
                                Console.WriteLine();
                            }
                        }

                        //Si le service n'est pas trouvé, on affiche un message d'erreur
                        if (!trouve)
                        {
                            Console.WriteLine();
                            Console.WriteLine("Le service n'existe pas dans le keychain.");
                            Console.WriteLine();
                            continue;
                        }
                    }
                    //Permet d'enlever un service en particulier
                    else if (commande == "enlever_service")
                    {
                        if (listeService != null && listeService.services.Count > 0)
                        {
                            bool trouve = false;
                            Console.WriteLine();
                            Console.Write("Veuillez entrer le nom du service: ");
                            string nom_service = Console.ReadLine();
                            Console.WriteLine();

                            for (int i = 0; i < listeService.services.Count; i++)
                            {
                                if (listeService.services[i].nom == nom_service)
                                {
                                    listeService.services.Remove(listeService.services[i]);
                                    trouve = true;
                                    Console.WriteLine("Le service a bel et bien été supprimé !");
                                    Console.WriteLine();
                                }
                            }

                            if (trouve == false)
                            {
                                Console.WriteLine("Ce service n'existe pas !");
                                Console.WriteLine();
                            }
                        }
                        else
                        {
                            Console.WriteLine();
                            Console.Write("Aucun service trouvé dans le keychain. Impossible de supprimer le service demandé.");
                            Console.WriteLine();
                            Console.WriteLine();
                        }
                    }
                    //Efface la console afin d'aider au niveau de la confidentialité
                    else if (commande == "effacer_console")
                    {
                        Console.Clear();
                    }
                    //Permet de retrouver la date/heure de dernière connexion
                    else if (commande == "derniere_connexion")
                    {
                        Console.WriteLine();
                        Console.WriteLine("Vous avez utilisé la dernière fois ce fichier le " + configuration.derniere_date_acces + ".");
                        Console.WriteLine();
                    }
                    //Affiche une aide expliquant les commandes gw_pass.
                    else if (commande == "aide")
                    {
                        Console.WriteLine();
                        Console.WriteLine("Voici les commandes qui sont disponibles");
                        Console.WriteLine();
                        Console.WriteLine("aide               | Affiche l'aide que vous voyez présentement.");
                        Console.WriteLine("ajouter_service    | Procédure pour ajouter un mot de passe du keychain.");
                        Console.WriteLine("credits            | Affiche plus d'informations concernant le concepteur de gw_pass.");
                        Console.WriteLine("derniere_connexion | Indique la dernière connexion réussie de gw_pass.");
                        Console.WriteLine("effacer_console    | Efface les lignes de commande de gw_pass.");
                        Console.WriteLine("enlever_service    | Enlève un service du keychain.");
                        Console.WriteLine("liste_service      | Procédure pour les services ayant été enregistré dans le keychain.");
                        Console.WriteLine("voir_service       | Procédure pour voir un des mots de passe du keychain.");
                        Console.WriteLine("quitter            | Ferme gw_pass.");
                        Console.WriteLine();
                    }
                    //Affiche plus d'information concernant le concepteur de gw_pass
                    else if (commande == "credits")
                    {
                        Console.WriteLine();
                        Console.WriteLine("Ce programme est la propriété intellectuelle de GreenWood Multimedia © 2021 - Tous droits réservés.");
                        Console.WriteLine("Écrit par Christopher Boisvert, propriétaire.");
                        Console.WriteLine();
                        Console.WriteLine("Pour en savoir plus sur GreenWood Multimedia");
                        Console.WriteLine("https://greenwoodmultimedia.com");
                        Console.WriteLine();
                    }
                    //Si aucune commande est reconnu, on affiche un message d'erreur.
                    else
                    {
                        Console.WriteLine();
                        Console.WriteLine("Commande '" + commande + "' non reconnu. Veuillez écrire 'aide' afin d'obtenir la liste des commandes possibles.");
                        Console.WriteLine();
                    }
                }
            }
            //Le programme se ferme an cas d'authentification échoué
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
            Console.WriteLine("--   gw_pass Version " + version + "   --");
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

                //Si la touche est le backspace, on efface une caractère.
                if(((int)key.Key) == 8)
                {
                    if(securePwd.Length > 0)
                    {
                        Console.CursorLeft -= 1;
                        Console.Write(" ");
                        Console.CursorLeft -= 1;
                        securePwd.RemoveAt(securePwd.Length - 1);
                    }
                }
                //Si la touche est ENTER, on saute l'itération actuelle de la boucle.
                else if(key.Key == ConsoleKey.Enter)
                {
                    continue;
                }
                else
                {
                    securePwd.AppendChar(key.KeyChar);
                    Console.Write("*");
                }
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

        /// <summary>
        /// S'occupe de transférer les données au bon endroit.
        /// </summary>
        /// <param name="configuration">Objet de type Configuration contenant la configuration de l'application.</param>
        /// <param name="nom_fichier_donnees">Chemin relatif ou absolu ou le fichier devrait être écrit.</param>
        /// <returns>Retourne vrai si tout c'est bien passé et faux dans le cas contraire.</returns>
        public static bool sauvegarder_donnees(Configuration configuration, string nom_fichier_donnees)
        {
            try
            {
                string configuration_json_data = JsonConvert.SerializeObject(configuration, Formatting.Indented);
                File.WriteAllBytes(@nom_fichier_donnees, Encoding.UTF8.GetBytes(configuration_json_data));
                return true;
            }
            catch(Exception exception)
            {
                //TODO: Idéalement, il faudrait logger l'erreur.
                return false;
            }
        }
    }
}
