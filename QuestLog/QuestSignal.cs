using Dalamud.IoC;
using Lumina.Excel;
using QuestRow = Lumina.Excel.Sheets.Quest;

namespace Umbra.QuestLog;

/// <summary>
/// Signal « une quête vient d'être terminée » : un numéro de version qui augmente,
/// que les widgets comparent au leur pour savoir s'il faut recalculer.
///
/// Source : IUnlockState.Unlock de Dalamud, déclenché avec la ligne Quest de chaque quête
/// nouvellement terminée (Dalamud vérifie après AgentUpdateFlag.UnlocksUpdate, regroupé à 500 ms).
/// Juste après la connexion, il se déclenche aussi pour toutes les quêtes déjà faites : la version
/// augmente beaucoup d'un coup, mais chaque widget ne recalcule qu'une fois.
/// Umbra n'enregistre pas IUnlockState dans son conteneur : on l'obtient avec
/// Framework.DalamudPlugin.Create&lt;T&gt;, qui injecte les propriétés [PluginService].
/// </summary>
internal static class QuestSignal
{
    private sealed class Services
    {
        [PluginService] public IUnlockState UnlockState { get; private set; } = null!;
    }

    private static int           _refs;
    private static IUnlockState? _unlocks;

    public static int Version { get; private set; }

    /// <summary>Faux si IUnlockState est introuvable : les widgets recalculent alors périodiquement.</summary>
    public static bool IsAvailable => _unlocks != null;

    public static void Acquire()
    {
        if (_refs++ > 0) return;

        try {
            _unlocks = Framework.DalamudPlugin.Create<Services>()?.UnlockState;
        } catch (Exception e) {
            Logger.Error($"[QuestLog] IUnlockState indisponible : {e.Message}");
            _unlocks = null;
        }

        if (_unlocks != null) _unlocks.Unlock += OnUnlock;

        var client = Framework.Service<IClientState>();
        client.Login  += OnLogin;
        client.Logout += OnLogout;
    }

    public static void Release()
    {
        if (_refs == 0 || --_refs > 0) return;

        if (_unlocks != null) _unlocks.Unlock -= OnUnlock;
        _unlocks = null;

        var client = Framework.Service<IClientState>();
        client.Login  -= OnLogin;
        client.Logout -= OnLogout;
    }

    private static void OnUnlock(RowRef rowRef)
    {
        if (rowRef.Is<QuestRow>()) Version++;
    }

    private static void OnLogin() => Version++;

    private static void OnLogout(int type, int code) => Version++;
}
