# gw_pass
Le logiciel gw_pass est un gestionnaire de clé qui est conçu pour être utilisé via la ligne de commande et de manière hors-ligne. 

Le programme utilise AES afin d'encrypter les mots de passe via une clé de 256 bits qui est généré à partir d'un mot de passe que l'utilisateur entre lors de la première utilisation.

L'implémentation d'AES utilise le mode CBC, éventuellement, ce mode sera changé pour un mode plus sécuritaire, car celui-ci est susceptible à des attaques dite d'oracle.