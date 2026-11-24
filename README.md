# Umbra – Quêtes

**Français** · [English](README.en.md)

Plugin pour [Umbra](https://github.com/una-xiv/umbra) (FINAL FANTASY XIV, via Dalamud) qui ajoute un widget de barre d'outils pour suivre la complétion des quêtes du journal, catégorie par catégorie. Il remplace le plugin [QuestTracker](https://github.com/keifufu/QuestTracker).

## Fonctionnalités

- **Dans la barre :** une icône et le pourcentage de quêtes faites (« Quêtes 82 % »), ou le nombre (« 1234/1500 »). Une barre de progression peut s'afficher sous le texte.
- **Option :** suivre une seule section (Scénario principal, Quêtes annexes…) ou une seule catégorie du journal au lieu du total.
- **Infobulle :** le total et les principales sections avec leur pourcentage.
- **Clic :** la liste des catégories du journal, regroupées par section, chacune avec une barre de progression et « faites / total ».
- **Clic sur une catégorie :** les quêtes qu'il reste à faire, avec leur niveau, le PNJ et la zone de départ, et un bouton retour.
- **Clic sur une quête :** pose le drapeau sur la carte à l'emplacement du PNJ de départ, ouvre la carte (option) et affiche un lien de carte dans le chat.
- **Options :** masquer les catégories à 100 %, masquer les quêtes des extensions pas encore atteintes dans le scénario principal.

Tout vient des données du jeu, sans aucun appel réseau. Le calcul est fait à la connexion, puis seulement quand une quête est terminée.

L'interface du widget, nom compris, suit la langue d'Umbra : en français si Umbra est en français, en anglais sinon. Les noms de quêtes, de PNJ, de zones et de catégories suivent la langue du client.

## Quêtes comptées

Le widget suit le journal du jeu : section → catégorie → genre → quête. Il laisse de côté les quêtes qu'on ne peut pas faire ou qui n'apparaissent pas dans le journal :

- les quasi-quêtes et missions hors journal ;
- les quêtes d'événements saisonniers, qui ne sont disponibles que pendant l'événement ;
- les quêtes retirées du jeu, sauf si le personnage les a faites ;
- les quêtes d'une autre cité de départ, et celles prévues pour ceux qui prennent plus tard la classe de départ du personnage ;
- les quêtes exclusives quand l'autre choix a été fait (par exemple « Call of the Wild » d'une autre Grande Compagnie), d'après les verrous définis dans les données du jeu ;
- les quêtes d'une autre Grande Compagnie que la sienne, ou de toutes les Grandes Compagnies tant que le personnage n'en a rejoint aucune ;
- les ordres de mission.

Une même quête du scénario de A Realm Reborn qui existe sous plusieurs identifiants (une par cité) n'est comptée qu'une fois. Les listes de quêtes de cité, de classe de départ, de doublons et de quêtes retirées viennent de QuestTracker.

Les totaux sont donc un peu plus bas que ceux de QuestTracker, qui compte aussi les événements saisonniers, les quasi-quêtes et les ordres de mission.

## Installation

1. Dans le jeu, ouvrez les **paramètres d'Umbra → Plugins**.
2. Ajoutez un dépôt GitHub : propriétaire `Soratix`, dépôt `UmbraQuestLog`.
3. Passez la barre d'outils en mode édition et ajoutez le widget **« Quêtes »** (« Quests » si Umbra n'est pas en français). Dans la recherche, vous pouvez aussi taper `quest`, `journal` ou `annexe`.

## Compiler soi-même

Il faut le SDK .NET 10 et XIVLauncher avec Umbra installé : les références pointent vers `%AppData%\XIVLauncher`.

```powershell
dotnet build -c Release QuestLog\QuestLog.csproj
```

## Crédits

Règles de regroupement et listes de quêtes particulières reprises de [QuestTracker](https://github.com/keifufu/QuestTracker), de keifufu (AGPL-3.0).

## Licence

AGPL-3.0, comme Umbra.
