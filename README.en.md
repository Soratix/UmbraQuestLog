# Umbra – Quests

[Français](README.md) · **English**

A toolbar widget for [Umbra](https://github.com/una-xiv/umbra) (FINAL FANTASY XIV, via Dalamud) that tracks your quest journal completion, category by category. It replaces the [QuestTracker](https://github.com/keifufu/QuestTracker) plugin.

## Features

- **In the toolbar:** an icon and the percentage of quests done ("Quests 82%"), or the count ("1234/1500"). A progress bar can be shown under the text.
- **Option:** track a single section (Main Scenario, Sidequests…) or a single journal category instead of the total.
- **Tooltip:** the total and the main sections with their percentage.
- **Click:** the journal categories, grouped by section, each with a progress bar and "done / total".
- **Click on a category:** the quests left to do, with their level, starting NPC and zone, and a back button.
- **Click on a quest:** places the map flag on the starting NPC, opens the map (optional) and prints a map link in the chat.
- **Options:** hide categories at 100%, hide quests from expansions not reached yet in the main scenario.

Everything comes from the game data, with no network calls. Completion is computed at login, then only when a quest is completed.

The widget's interface, name included, follows Umbra's language: French when Umbra is in French, English otherwise. Quest, NPC, zone and category names follow your client's language.

## Which quests are counted

The widget follows the game's journal: section → category → genre → quest. It leaves out quests that cannot be done or do not appear in the journal:

- quasi-quests and missions outside the journal;
- seasonal event quests, only available during the event;
- quests removed from the game, unless the character completed them;
- quests for another starting city, and the ones meant for players who pick up the character's starting class later;
- mutually exclusive quests once the other choice was made (for example another Grand Company's "Call of the Wild"), based on the locks defined in the game data, along with the quests that follow them (for example the follow-up to enlisting in another Grand Company);
- quests for a Grand Company other than yours, or for every Grand Company until the character joins one;
- levequests.

A Realm Reborn main scenario quests that exist under several IDs (one per city) are counted once. The lists of city quests, starting class quests, duplicates and removed quests come from QuestTracker.

Totals are therefore a bit lower than QuestTracker's, which also counts seasonal events, quasi-quests and levequests.

## Installation

1. In game, open **Umbra settings → Plugins**.
2. Add a GitHub repository: owner `Soratix`, repository `UmbraQuestLog`.
3. Switch the toolbar to edit mode and add the **Quests** widget (« Quêtes » when Umbra is in French). You can also search for `quest`, `journal` or `sidequest`.

## Building it yourself

You need the .NET 10 SDK and XIVLauncher with Umbra installed: the references point to `%AppData%\XIVLauncher`.

```powershell
dotnet build -c Release QuestLog\QuestLog.csproj
```

## Credits

Grouping rules and special quest lists taken from [QuestTracker](https://github.com/keifufu/QuestTracker) by keifufu (AGPL-3.0).

## License

AGPL-3.0, like Umbra.
