using Newtonsoft.Json;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Security;
using System.Reflection;
using System.Diagnostics;
using gw_pass.Classes;
using System.Collections.Generic;

namespace gw_pass
{
    /// <summary>
    /// Classe principale de gw_pass.
    /// </summary>
    class Program
    {

        private const string NOM_APP = "GW PASS - Votre gestionnaire de clé portatif !";
        private const string MESSAGE_ACCUEIL = "Bienvenue sur votre keychain portatif !";
        private const string EXPLICATION_ARGUMENTS_1 = "- Vous devez spécifier le premier argument !";
        private const string EXPLICATION_ARGUMENTS_2 = "- Celui-ci doit être doit être un chemin absolu vers le dossier auquel vous voulez stocker les mot de passe.";
        private const string EXPLICATION_ARGUMENTS_3 = "- Vous devrez spécifier à chaque fois que vous voulez récuperer les données.";
        private const string EXPLICATION_ARGUMENTS_4 = "- Exemple : gw_passe.exe C:\\mondossier";
        private const string NOM_FICHIER_DEFAUT = "\\gw_pass.json";
        private const string DEMANDER_QUITTER_APPLICATION = "Veuillez pressez une touche pour quitter...";
        private const string DEMANDER_CLE_ENCRYPTION = "Veuillez entrer votre clé de décryption: ";
        private const string MESSAGE_AUTHENTIFICATION = "Vous êtes authentifié !";
        private const string MESSAGE_ERREUR_AUTHENTIFICATION = "Votre authentification a échoué ! Le programme va se fermer après que vous toucher sur une touche...";
        private const string PREFIXE_COMMANDE_CONSOLE = "gw_pass>";

        /// <summary>
        /// Point d'entrée du programme.
        /// </summary>
        /// <param name="args">Arguments du programme</param>
        static void Main(string[] args)
        {
            //Changement du titre de la console
            Console.Title = NOM_APP;

            //Affiche l'en-tête du programme gw_pass
            en_tete();

            //Message de bienvenue
            ecrire_texte_console(MESSAGE_ACCUEIL, 0, 0);

            if (args.Length == 0)
            {
                ecrire_texte_console(EXPLICATION_ARGUMENTS_1, 1, 0);
                ecrire_texte_console(EXPLICATION_ARGUMENTS_2, 0, 0);
                ecrire_texte_console(EXPLICATION_ARGUMENTS_3, 0, 0);
                ecrire_texte_console(EXPLICATION_ARGUMENTS_4, 0, 1);
                Console.Write(DEMANDER_QUITTER_APPLICATION);
                Console.ReadKey();
                return;
            }

            //Variables du programme
            SecureString cle_decryption_utilisateur = null;
            GestionAes gestionAes = null;
            ConfigurationV2 configuration = null;

            //Variables concernant le statut du programme
            bool authentifier = false;
            string nom_fichier_donnees = args[0] + NOM_FICHIER_DEFAUT;

            //On va chercher la configuration du fichier de données
            if (File.Exists(nom_fichier_donnees))
            {
                //On va chercher le contenu du fichier de configuration
                string contenu_fichier_donnees = File.ReadAllText(nom_fichier_donnees);

                //On désérialize le json du fichier
                try
                {
                    configuration = JsonConvert.DeserializeObject<ConfigurationV2>(contenu_fichier_donnees);
                }
                catch
                {
                    Configuration configurationBase = JsonConvert.DeserializeObject<Configuration>(contenu_fichier_donnees);

                    ecrire_texte_console("Votre fichier de configuration est écrit dans une version antérieure à la version de gw_pass que vous utiliser.", 1, 1);
                    ecrire_texte_console("Version actuelle de gw_pass: " + version(), 0, 0);
                    ecrire_texte_console("Version du fichier gw_pass.json: " + configurationBase.version, 0, 1);
                    ecrire_texte_console("Votre mot de passe sera requis afin de pouvoir effectuer la conversion du fichier.", 0, 1);
                    ecrire_ligne("Veuillez entrer votre mot de passe: ");
                    cle_decryption_utilisateur = obtenir_mot_de_passe();
                    ecrire_texte_console("", 0, 0);

                    if (GestionAes.sha_256(cle_decryption_utilisateur) == configurationBase.mot_de_passe)
                    {
                        gestionAes = new GestionAes(cle_decryption_utilisateur, configurationBase.sel);

                        if (configurationBase.obtenir_numero_version()[0] < 2)
                        {
                            ConfigurationV1 configurationV1 = JsonConvert.DeserializeObject<ConfigurationV1>(contenu_fichier_donnees);

                            ListeService liste_service = JsonConvert.DeserializeObject<ListeService>(gestionAes.decrypter(configurationV1.liste_service));

                            for(int i = 0; i < liste_service.services.Count; i++)
                            {
                                liste_service.services[i].nom = gestionAes.encrypter(liste_service.services[i].nom);
                                liste_service.services[i].identifiant = gestionAes.encrypter(liste_service.services[i].identifiant);
                                liste_service.services[i].mot_de_passe = gestionAes.encrypter(liste_service.services[i].mot_de_passe);
                            }

                            //Création de l'objet qui représentera la configuration
                            configuration = new ConfigurationV2
                            {
                                version = version(),
                                courriel = configurationV1.courriel,
                                mot_de_passe = GestionAes.sha_256(cle_decryption_utilisateur),
                                sel = configurationV1.sel,
                                date_initialisation = DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss"),
                                derniere_date_acces = gestionAes.encrypter(DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss")),
                                liste_service = liste_service.services
                            };

                            /////////////SAUVEGARDE//////////////////

                            //Sauvegarde des données de l'application
                            bool succes = sauvegarder_donnees(configuration, nom_fichier_donnees);

                            //Si une erreur survient lors du write du fichier de config, on doit stopper l'éxécution. 
                            if (!succes)
                            {
                                ecrire_texte_console("Une erreur est survenue lors de la tentative d'écriture du fichier de configuration.", 1, 1);

                                //On sort du programme, car il n'a rien à faire.
                                return;
                            }

                            //Succes !
                            ecrire_texte_console("Nouveau fichier de configuration par défaut créer et encrypté !", 0, 0);
                            authentifier = true;
                        }
                        else
                        {
                            ecrire_texte_console("Une erreur est survenue !", 1, 1);
                            return;
                        }
                    }
                    else
                    {
                        ecrire_texte_console("Vous n'avez pas entré le bon mot de passe !", 1, 1);
                        return;
                    }
                }

                //On affiche un message de succès
                ecrire_texte_console("Votre fichier de configuration a été trouvé.", 0, 0);
            }
            //S'il n'existe pas, on va le créer
            else
            {
                ///OBTENTION DU MOT DE PASSE///

                //On n'a pas trouvé le fichier de configuration, alors on va le créer.
                ecrire_texte_console("Aucun fichier de configuration n'a été trouvé. Nous allons en créer un avec vous.", 0, 1);

                //On entre le courriel de l'utilisateur
                ecrire_texte_console("Veuillez entrer un courriel (sert en cas d'oubli de mot de passe ", 0, 0);
                ecrire_ligne("et est utilisé comme second facteur d'authentification): ");
                string courriel = Console.ReadLine();
                ecrire_texte_console("", 0, 0);

                //On entre la clé de décryption par l'utilisateur
                ecrire_ligne("Veuillez entrer un mot de passe qui sera utilisé pour l'encryption: ");
                cle_decryption_utilisateur = obtenir_mot_de_passe();
                ecrire_texte_console("", 0, 0);

                /////////////INITIALISATION DES VARIABLES//////////////////

                //Création du sel unique à chaque création de fichier encrypté
                byte[] sel_random = new byte[8];
                using (RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider())
                {
                    //Rempli le tableau de byte null
                    rngCsp.GetBytes(sel_random);
                }

                //Création de l'objet de gestion AES.
                gestionAes = new GestionAes(cle_decryption_utilisateur, sel_random);

                //Création de l'objet qui représentera la configuration
                configuration = new ConfigurationV2
                {
                    version = version(),
                    courriel = gestionAes.encrypter(courriel),
                    mot_de_passe = GestionAes.sha_256(cle_decryption_utilisateur),
                    sel = sel_random,
                    date_initialisation = DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss"),
                    derniere_date_acces = gestionAes.encrypter(DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss")),
                    liste_service = new List<Service>()
                };

                /////////////SAUVEGARDE//////////////////

                //Sauvegarde des données de l'application
                bool succes = sauvegarder_donnees(configuration, nom_fichier_donnees);

                //Si une erreur survient lors du write du fichier de config, on doit stopper l'éxécution. 
                if(!succes)
                {
                    ecrire_texte_console("Une erreur est survenue lors de la tentative d'écriture du fichier de configuration.", 1, 1);

                    //On sort du programme, car il n'a rien à faire.
                    return;
                }

                //Succes !
                ecrire_texte_console("Fichier de configuration par défaut créer et encrypté !", 0, 0);
            }

            /////////////AUTHENFICATION//////////////////

            //On redemande la clé de décryption afin d'authentifier l'utilisateur
            ecrire_texte_console("", 0, 0);
            ecrire_ligne(DEMANDER_CLE_ENCRYPTION);
            cle_decryption_utilisateur = obtenir_mot_de_passe();
            ecrire_texte_console("", 0, 0);

            //Vérification de la clé de décryption afin d'authentifier l'utilisateur
            if (GestionAes.sha_256(cle_decryption_utilisateur) == configuration.mot_de_passe || authentifier)
            {
                if(gestionAes == null)
                {
                    gestionAes = new GestionAes(cle_decryption_utilisateur, configuration.sel);
                }

                Console.Clear();

                //On flag l'utilisateur comme authentifier
                authentifier = true;

                //Message destiné à l'utilisateur à sa connexion
                ecrire_texte_console(MESSAGE_AUTHENTIFICATION , 1, 0);
                ecrire_texte_console("Vous avez utilisé la dernière fois ce fichier le " + gestionAes.decrypter(configuration.derniere_date_acces) + ".", 0, 1);

                //On met à jour la date d'utilisation après avoir afficher la dernière date
                configuration.derniere_date_acces = gestionAes.encrypter(DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss"));

                //On entre dans la section commune du programme.
                while (authentifier)
                {
                    //Invite de commande de gw_pass
                    ecrire_ligne(PREFIXE_COMMANDE_CONSOLE);

                    //Lecture de la commande
                    string commande = Console.ReadLine();

                    //Permet de quitter le programme.
                    if (commande == "quitter" || commande == "q")
                    {
                        //On enregistre le data et dans la prochaine boucle, le programme se termine.
                        sauvegarder_donnees(configuration, nom_fichier_donnees);

                        //On sort de la loop
                        break;
                    }
                    //Affiche la liste des services.
                    else if (commande == "liste_service" || commande == "ls")
                    {
                        if (configuration.liste_service != null && configuration.liste_service.Count > 0)
                        {
                            //On va trier les services
                            if (configuration.liste_service.Count > 1)
                            {
                                configuration.trier_liste_service(gestionAes);
                            }

                            ecrire_texte_console("Voici la liste des services trouvés : ", 1, 1);

                            for (int i = 0; i < configuration.liste_service.Count; i++)
                            {
                                Console.WriteLine("- " + gestionAes.decrypter(configuration.liste_service[i].nom));
                            }
                            ecrire_texte_console("", 0, 0);
                        }
                        else
                        {
                            ecrire_texte_console("Aucun service trouvé. Veuillez en ajouter un dans le gestionnaire de clé.", 1, 1);
                        }
                    }
                    //Permet de voir un service en particulier
                    else if (commande == "voir_service" || commande == "vs")
                    {
                        if (configuration.liste_service != null && configuration.liste_service.Count > 0)
                        {
                            bool trouve = false;

                            ecrire_texte_console("", 0, 0);
                            ecrire_ligne("Veuillez entrer le nom du service: ");
                            string nom_service = Console.ReadLine();

                            for (int i = 0; i < configuration.liste_service.Count; i++)
                            {
                                if (configuration.liste_service[i].nom == gestionAes.encrypter(nom_service))
                                {
                                    //On affiche une en-tête aux informations du service
                                    ecrire_texte_console("INFORMATIONS DU SERVICE", 1, 0);

                                    //Affichage des données via l'appel de afficher_service
                                    ecrire_texte_console("Nom du service: " + gestionAes.decrypter(configuration.liste_service[i].nom), 0, 0);
                                    ecrire_texte_console("Identifiant: " + gestionAes.decrypter(configuration.liste_service[i].identifiant), 0, 0);
                                    ecrire_texte_console("Mot de passe: " + gestionAes.decrypter(configuration.liste_service[i].mot_de_passe), 0, 1);

                                    //On indique qu'on a trouvé le service
                                    trouve = true;
                                }
                            }

                            if(trouve == false)
                            {
                                ecrire_texte_console("Ce service n'existe pas !", 1, 1);
                            }
                        }
                        else
                        {
                            ecrire_texte_console("Aucun service trouvé. Veuillez en ajouter un dans le gestionnaire de clé.", 1, 1);
                        }
                    }
                    //Permet d'ajouter un service en particulier
                    else if (commande == "ajouter_service" || commande == "as")
                    {
                        bool trouve = false;

                        //On demande le nom du nouveau service
                        ecrire_texte_console("", 0, 0);
                        ecrire_ligne("Veuillez entrer le nom du service: ");
                        string nom_service = Console.ReadLine();

                        //On va chercher pour voir si le service existe déjà
                        for (int i = 0; i < configuration.liste_service.Count; i++)
                        {
                            if (configuration.liste_service[i].nom == gestionAes.encrypter(nom_service))
                            {
                                trouve = true;
                            }
                        }

                        //Si le service existe déjà, on afficher une erreur
                        if (trouve)
                        {
                            ecrire_texte_console("Un service du même nom existe déjà ! Le service demandé ne sera pas ajouté !", 1, 1);
                            continue;
                        }

                        //Sinon, on continue et on demande les informations du service
                        ecrire_ligne("Veuillez entrer le courriel du service: ");
                        string identifiant = Console.ReadLine();

                        ecrire_ligne("Veuillez entrer le mot de passe du service: ");
                        string mot_de_passe = Console.ReadLine();

                        //Création de l'objet Service
                        Service nouveau_service = new Service()
                        {
                            nom = gestionAes.encrypter(nom_service),
                            identifiant = gestionAes.encrypter(identifiant),
                            mot_de_passe = gestionAes.encrypter(mot_de_passe)
                        };

                        //Ajout de l'objet à la liste de service
                        configuration.liste_service.Add(nouveau_service);

                        //On va trier les services
                        if(configuration.liste_service.Count > 1)
                        {
                            configuration.trier_liste_service(gestionAes);
                        }

                        //On va sauvegarder les données en cas de crash/fermeture innattendue
                        bool succes_sauvegarde = sauvegarder_donnees(configuration, nom_fichier_donnees);

                        if (succes_sauvegarde)
                        {
                            //Affichage du succès de l'opération
                            ecrire_texte_console("Le service a été ajouté avec succès !", 1, 1);
                        }
                        else
                        {
                            //On va le supprimer afin de garantir l'intégrité des données.
                            configuration.liste_service.Remove(nouveau_service);

                            //Une erreur dans la sauvegarde du fichier est survenue
                            ecrire_texte_console("Une erreur innatendue est survenue. Le service n'a pas été ajouté.", 1, 2);
                        }
                    }
                    //Permet de changer d'un service
                    else if(commande == "changer_service" || commande == "cs")
                    {
                        bool trouve = false;

                        //On va demander le service à trouver
                        ecrire_texte_console("", 0, 0);
                        ecrire_ligne("Veuillez entrer le nom du service à modifier: ");
                        string nom_service = Console.ReadLine();

                        //On va tenter de trouver le service
                        for (int i = 0; i < configuration.liste_service.Count; i++)
                        {
                            if (configuration.liste_service[i].nom == gestionAes.encrypter(nom_service))
                            {
                                trouve = true;

                                //Avant d'effectuer une quelquonque opération, nous allons prendre une copie du service
                                Service copieServiceEnCasErreur = configuration.liste_service[i];

                                //Affichage des anciennes informations du service
                                ecrire_texte_console("INFORMATIONS DU SERVICE AVANT MODIFICATIONS", 1, 0);

                                //Affichage des données via l'appel de afficher_service
                                ecrire_texte_console("Nom du service: " + gestionAes.decrypter(configuration.liste_service[i].nom), 0, 0);
                                ecrire_texte_console("Identifiant: " + gestionAes.decrypter(configuration.liste_service[i].identifiant), 0, 0);
                                ecrire_texte_console("Mot de passe: " + gestionAes.decrypter(configuration.liste_service[i].mot_de_passe), 0, 0);

                                //Obtention des nouvelles données
                                ecrire_texte_console("", 0, 0);
                                ecrire_ligne("Veuillez entrer le nouveau nom du service: ");
                                string nouveau_nom_service = Console.ReadLine();
                                ecrire_ligne("Veuillez entrer le nouvel identifiant du service: ");
                                string nouveau_identifiant_service = Console.ReadLine();
                                ecrire_ligne("Veuillez entrer le nouveau mot de passe du service: ");
                                string nouveau_mot_de_passe = Console.ReadLine();

                                //On modifie l'objet de la liste des services
                                configuration.liste_service[i].nom = gestionAes.encrypter(nouveau_nom_service);
                                configuration.liste_service[i].identifiant = gestionAes.encrypter(nouveau_identifiant_service);
                                configuration.liste_service[i].mot_de_passe = gestionAes.encrypter(nouveau_mot_de_passe);

                                //On va trier les services, si il y a plus d'un élément
                                if (configuration.liste_service.Count > 1)
                                {
                                    configuration.trier_liste_service(gestionAes);
                                }

                                //On va sauvegarder les données en cas de crash/fermeture innattendue
                                bool succes_sauvegarde = sauvegarder_donnees(configuration, nom_fichier_donnees);

                                if (succes_sauvegarde)
                                {
                                    //Affichage du succès de l'opération
                                    ecrire_texte_console("Le service a été modifié avec succès !", 1, 1);
                                }
                                else
                                {
                                    //On va remettre le vieux service afin de garantir l'intégrité des données.
                                    configuration.liste_service[i] = copieServiceEnCasErreur;

                                    //Une erreur dans la sauvegarde du fichier est survenue
                                    ecrire_texte_console("Une erreur innattendue est survenue. Le service n'a pas été modifié.", 1, 2);
                                }
                            }
                        }

                        //Si le service n'est pas trouvé, on affiche un message d'erreur
                        if (!trouve)
                        {
                            ecrire_texte_console("Le service n'existe pas dans le keychain.", 1, 1);
                            continue;
                        }
                    }
                    //Affiche des informations concernant l'installation actuelle du programme
                    else if (commande == "configuration" || commande == "conf")
                    {
                        ecrire_texte_console("Identifiant de l'utilisateur                  | " + gestionAes.decrypter(configuration.courriel), 1, 0);
                        ecrire_texte_console("Date de création du fichier de données        | " + configuration.date_initialisation, 0, 0);
                        ecrire_texte_console("Chemin absolu du fichier de données           | " + nom_fichier_donnees, 0, 1);
                    }
                    //Permet d'enlever un service en particulier
                    else if (commande == "enlever_service" || commande == "es")
                    {
                        if (configuration.liste_service != null && configuration.liste_service.Count > 0)
                        {
                            bool trouve = false;
                            ecrire_texte_console("", 0, 0);
                            ecrire_ligne("Veuillez entrer le nom du service: ");
                            string nom_service = Console.ReadLine();
                            ecrire_texte_console("", 0, 0);

                            for (int i = 0; i < configuration.liste_service.Count; i++)
                            {
                                if (configuration.liste_service[i].nom == gestionAes.encrypter(nom_service))
                                {
                                    Service copieServiceEnCasErreur = configuration.liste_service[i];

                                    configuration.liste_service.Remove(configuration.liste_service[i]);
                                    trouve = true;

                                    if (configuration.liste_service.Count > 1)
                                    {
                                        configuration.liste_service.Sort();
                                    }

                                    //On va sauvegarder les données en cas de crash/fermeture innattendue
                                    bool succes_sauvegarde = sauvegarder_donnees(configuration, nom_fichier_donnees);

                                    if (succes_sauvegarde)
                                    {
                                        //Affichage du succès de l'opération
                                        ecrire_texte_console("Le service a été supprimé avec succès !", 0, 1);
                                    }
                                    else
                                    {
                                        //On va remettre le vieux service afin de garantir l'intégrité des données.
                                        configuration.liste_service[i] = copieServiceEnCasErreur;

                                        //Une erreur dans la sauvegarde du fichier est survenue
                                        ecrire_texte_console("Une erreur innattendue est survenue. Le service n'a pas été supprimé.", 1, 2);
                                    }
                                }
                            }

                            if (trouve == false)
                            {
                                ecrire_texte_console("Ce service n'existe pas !", 0, 1);
                            }
                        }
                        else
                        {
                            ecrire_texte_console("Aucun service trouvé dans le keychain. Impossible de supprimer le service demandé.", 1, 2);
                        }
                    }
                    //Efface la console afin d'aider au niveau de la confidentialité
                    else if (commande == "effacer_console" || commande == "eff")
                    {
                        Console.Clear();
                    }
                    //Permet de retrouver la date/heure de dernière connexion
                    else if (commande == "derniere_connexion" || commande == "dc")
                    {
                        ecrire_texte_console("Vous avez utilisé la dernière fois ce fichier le " + gestionAes.decrypter(configuration.derniere_date_acces) + ".", 1, 1);
                    }
                    //Affiche une aide expliquant les commandes gw_pass.
                    else if (commande == "aide" || commande == "a")
                    {
                        ecrire_texte_console("                      Aide                                 ", 1, 1);
                        ecrire_texte_console("Nom de la commande | Raccourci | Description de la commande", 0, 1);

                        ecrire_texte_console("aide               | a         | Affiche l'aide que vous voyez présentement.", 0, 0);
                        ecrire_texte_console("credits            | crd       | Affiche les crédits concernant gw_pass.", 0, 0);
                        ecrire_texte_console("configuration      | conf      | Affiche plus d'informations concernant votre installation de gw_pass.", 0, 0);
                        ecrire_texte_console("derniere_connexion | dc        | Indique la dernière connexion réussie de gw_pass.", 0, 0);
                        ecrire_texte_console("effacer_console    | eff       | Efface la console.", 0, 0);
                        ecrire_texte_console("quitter            | q         | Ferme gw_pass.", 0, 0);

                        ecrire_texte_console("                Gestion des services                       ", 1, 1);

                        ecrire_texte_console("liste_service      | ls        | Procédure pour voir tous les services ayant été enregistré dans le gestionnaire de clés.", 0, 0);
                        ecrire_texte_console("voir_service       | vs        | Procédure pour voir un des mots de passe du gestionnaire de clés.", 0, 0);
                        ecrire_texte_console("ajouter_service    | as        | Procédure pour ajouter un mot de passe du gestionnaire de clés.", 0, 0);
                        ecrire_texte_console("changer_service    | cs        | Procéder pour changer un service du gestionnaire de clés.", 0, 0);
                        ecrire_texte_console("enlever_service    | es        | Enlève un service du gestionnaire de clés.", 0, 1);
                    }
                    //Affiche plus d'information concernant le concepteur de gw_pass
                    else if (commande == "credits" || commande == "crd")
                    {
                        ecrire_texte_console("GW PASS - Version " + version(), 1, 1);
                        ecrire_texte_console("Ce programme est la propriété intellectuelle de GreenWood Multimedia © 2021 - Tous droits réservés.", 0, 0);
                        ecrire_texte_console("Écrit par Christopher Boisvert, propriétaire.", 0, 0);
                        ecrire_texte_console("Pour en savoir plus sur GreenWood Multimedia", 1, 0);
                        ecrire_texte_console("https://greenwoodmultimedia.com", 0, 1);
                    }
                    //Si aucune commande est reconnu, on affiche un message d'erreur.
                    else
                    {
                        ecrire_texte_console(" Commande '" + commande + "' non reconnu. Veuillez écrire 'aide' afin d'obtenir la liste des commandes possibles.", 1, 1);
                    }
                }
            }
            //Le programme se ferme an cas d'authentification échoué
            else
            {
                ecrire_texte_console(MESSAGE_ERREUR_AUTHENTIFICATION, 0, 1);
            }
        }

        /// <summary>
        /// Affiche l'en-tête du programme.
        /// </summary>
        /// <param name="version">Version du programme.</param>
        public static void en_tete()
        {
            ecrire_texte_console("-------------------------------", 1, 0);
            ecrire_texte_console("--                           --", 0, 0);
            ecrire_texte_console("--    GreenWood Multimedia   --", 0, 0);
            ecrire_texte_console("--   gw_pass Version " + version() + "   --", 0, 0);
            ecrire_texte_console("--                           --", 0, 0);
            ecrire_texte_console("--          © " + DateTime.Now.ToString("yyyy") + "           --", 0, 0);
            ecrire_texte_console("--                           --", 0, 0);
            ecrire_texte_console("--    Tous droits réservés   --", 0, 0);
            ecrire_texte_console("--                           --", 0, 0);
            ecrire_texte_console("-------------------------------", 0, 1);
        }

        /// <summary>
        /// Fonction qui permet de retourner la version du programme.
        /// </summary>
        /// <returns>Retourne la version actuelle du programme.</returns>
        public static string version()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fileVersionInfo.ProductVersion;
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
            ecrire_texte_console("", 0, 0);

            return securePwd;
        }

        /// <summary>
        /// Enregistre/Encrypte les données dans un fichier json.
        /// </summary>
        /// <param name="configuration">Objet de type Configuration initialisé au début du programme.</param>
        /// <param name="cle_decryption_utilisateur">Clé de décryption de l'utilisateur utilisé afin d'encrypter les données.</param>
        /// <param name="nom_fichier_donnees">Chemin relatif par défaut ou le fichier se doit d'être enregistré.</param>
        /// <returns>Retourne vrai si tout a réussie et faux dans le cas contraire.</returns>
        public static bool sauvegarder_donnees(ConfigurationV2 configuration, string nom_fichier_donnees)
        {
            //On va tenter d'écrire les changements dans le fichier de sauvegarde
            try
            {
                //On va convertir l'objet de configuration en JSON
                string configuration_json_data = JsonConvert.SerializeObject(configuration, Formatting.Indented);

                //On va tenter d'écrire ce fichier à l'endroit indiquer par l'utilisateur
                File.WriteAllBytes(@nom_fichier_donnees, Encoding.UTF8.GetBytes(configuration_json_data));

                ///On va supprimer la valeur du JSON, car elle n'est plus nécessaire
                configuration_json_data = null;

                //Si tout ceci c'est bien effectué, on va retourner vrai
                return true;
            }
            catch(Exception)
            {
                //TODO: Idéalement, il faudrait logger l'erreur.
                //Dans le cas contraire, on va retourner faux
                return false;
            }
        }

        /// <summary>
        /// Méthode permet d'écrire une ligne à la console avec des espaces avant ou après.
        /// </summary>
        /// <param name="contenu">Contenu à écrire à la console.</param>
        /// <param name="nombre_espace_avant">Nombre de lignes à mettre avant le contenu.</param>
        /// <param name="nombre_espace_apres">Nombre de lignes à mettre après le contenu.</param>
        public static void ecrire_texte_console(string contenu, int nombre_espace_avant, int nombre_espace_apres)
        {
            if(nombre_espace_avant > 0)
            {
                for (int i = 0; i < nombre_espace_avant; i++)
                {
                    Console.WriteLine();
                }
            }
            Console.WriteLine(" " + contenu);
            if (nombre_espace_apres > 0)
            {
                for (int i = 0; i < nombre_espace_apres; i++)
                {
                    Console.WriteLine();
                }
            }
        }

        /// <summary>
        /// Méthode permettant d'écrire une ligne à la console.
        /// </summary>
        /// <param name="contenu"></param>
        public static void ecrire_ligne(string contenu)
        {
            Console.Write(" " + contenu);
        }
    }
}
