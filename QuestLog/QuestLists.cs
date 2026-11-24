using System.Collections.Generic;

namespace Umbra.QuestLog;

/// <summary>
/// Ce que les feuilles du jeu ne disent pas, repris de QuestTracker
/// (https://github.com/keifufu/QuestTracker, keifufu, AGPL-3.0) : quêtes réservées à une cité
/// de départ, première quête de la classe de départ, doublons du scénario de ARR et quêtes retirées.
/// </summary>
internal static class QuestLists
{
    /// <summary>Quêtes « Coming to … » : celle qui est faite donne la cité de départ.</summary>
    public const uint ComingToGridania = 65575;
    public const uint ComingToLimsa    = 65643;
    public const uint ComingToUldah    = 66130;

    /// <summary>« Call of the Sea » : commune aux départs de Gridania et Limsa Lominsa (Ul'dah a la sienne).</summary>
    public const uint CallOfTheSea = 66210;

    public static readonly HashSet<uint> GridaniaStart = [
        65621, 65659, 65660, 65564, 65737, 65981, 65664, 69390, 65711, 65661, 69391,
        65665, 65712, 65912, 65913, 65915, 65916, 65917, 65920, 65923, 65697, 65982,
        65983, 65984, 65985, 66043, 65575, 65537, 65568, 65570, 65573, 65756, 65708,
        65663, 65666, 65596, 65914,
    ];

    public static readonly HashSet<uint> LimsaStart = [
        65644, 65645, 65998, 65999, 66079, 66001, 66002, 66003, 66004, 66005, 65933,
        65938, 65939, 65942, 65948, 65951, 65949, 65950, 66225, 66080, 66226, 66081,
        66082, 65643, 65647, 65648, 65658, 66199, 66229, 66008, 66006, 66009, 66010,
        65936, 65937, 66011, 66012, 66013, 66022, 66014, 66015, 65595, 65941,
    ];

    public static readonly HashSet<uint> UldahStart = [
        66104, 66105, 66106, 66131, 66207, 66086, 65839, 65842, 69388, 65843, 65856,
        66159, 65864, 66039, 65865, 65866, 65867, 69389, 65868, 65869, 65870, 65872,
        66164, 66087, 66177, 66088, 66064, 66209, 66130, 65925, 65926, 66223, 65594,
        65877, 66040, 66042, 65878, 66041, 65857, 65840, 65858, 65924, 65844, 65862,
        66067, 66066, 66109,
    ];

    /// <summary>
    /// Classe de départ : (première quête de la classe faite au départ, quêtes de la même classe
    /// prévues pour ceux qui la prennent plus tard, à exclure si la première est faite et pas elles).
    /// </summary>
    public static readonly (uint StartQuest, uint[] Excluded)[] StartClass = [
        (65792, [65822, 65713]), // Gladiateur
        (66090, [66089, 65714]), // Pugiliste
        (65849, [65848, 65715]), // Maraudeur
        (65583, [65754, 65716]), // Maître d'hast
        (65582, [65755, 65717]), // Archer
        (65640, [65638, 65637]), // Surineur
        (65584, [65747, 65718]), // Élémentaliste
        (65883, [65882, 65719]), // Occultiste
        (65991, [65990, 65987]), // Arcaniste
    ];

    /// <summary>Même quête sous plusieurs ID (une par cité ou version retirée) : comptée une fois.</summary>
    public static readonly uint[][] DuplicateGroups = [
        [65621, 65659, 65660], [65644, 65645], [66104, 66105, 66106], [65664, 69390], [65842, 69388],
        [65661, 69391], [65867, 69389], [65781, 66211], [66244, 69392], [66253, 69393], [66254, 69394],
        [66262, 69395], [66269, 69396], [66270, 69397], [66276, 69398], [66320, 69399], [66321, 69400],
        [66355, 69401], [66375, 69402], [66408, 69403], [66453, 69404], [66473, 69405], [66504, 69406],
        [66539, 69407], [66572, 70057], [66579, 69408], [66672, 69409], [66060, 70058], [66712, 69410],
        [66714, 69411], [66716, 69412], [66724, 69413], [66729, 69414], [66881, 69415], [66884, 69416],
        [66886, 69417], [66889, 69418], [66980, 69419], [66981, 69420], [66988, 69421], [65615, 69422],
        [65617, 69423], [65903, 69424], [65955, 70127], [66735, 67245], [67823, 66097], [66552, 67089],
        [65821, 65789], [65797, 65824], [66069, 66068], [66091, 66234], [65847, 65846], [65850, 65851],
        [65668, 65559], [65571, 65679], [65667, 65557], [65669, 65558], [65683, 65627], [65881, 65880],
        [65884, 65885], [65988, 65989], [65993, 65992], [68753, 69569],
    ];

    /// <summary>Quêtes retirées du jeu : comptées seulement si le personnage les a faites.</summary>
    public static readonly HashSet<uint> Retired = [
        65603, 65616, 65692, 65695, 65732, 65734, 65841, 65860, 65863, 65871, 65910,
        65918, 65934, 65940, 66000, 66023, 66033, 66034, 66288, 66351, 66352, 66356,
        66383, 66390, 66407, 66413, 66417, 66432, 66461, 66462, 66490, 66507, 66510,
        66575, 66578, 66582, 66713, 66715, 66717, 66718, 66719, 66720, 66721, 66722,
        66723, 66885, 66887, 66890, 66891, 66893, 66964, 66965, 66985, 66986, 66987,
        66990, 66991, 67097, 67098, 67635, 67653, 67752, 67819, 67870, 68629, 68727,
        69296, 69377, 69508, 69578, 71000, 71001, 71003, 71004,
    ];
}
