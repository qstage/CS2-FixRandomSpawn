using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Cvars;

namespace FixRandomSpawn;

public sealed partial class Plugin
{
    private HookResult OnRoundPrestart(EventRoundPrestart _, GameEventInfo _1) => OnRoundPrestart();

    private HookResult OnRoundPrestart()
    {
        if (!Config.WarmupMode.Enable)
            return HookResult.Continue;

        int value = Convert.ToInt32(_gameRules.WarmupPeriod);

        ConVar[] convars = [
            ConVar.Find("mp_randomspawn")!,
            ConVar.Find("mp_buy_anywhere")!,
        ];

        if (convars[0].GetPrimitiveValue<int>() != 0 || value != 0)
        {
            convars[0]!.SetValue(value);
        }

        if (Config.WarmupMode.BuyAnywhere && (convars[1].GetPrimitiveValue<int>() != 0 || value != 0))
        {
            Server.ExecuteCommand($"mp_buy_anywhere {value}");
        }

        return HookResult.Continue;
    }

    private void OnClientPutInServer(int playerSlot)
    {
        var player = Utilities.GetPlayerFromSlot(playerSlot);

        if (player == null || player.IsBot) return;

        if (_gameRules.WarmupPeriod && Config.WarmupMode.Enable && Config.WarmupMode.AlertForPlayers)
        {
            player.PrintToChat(Localizer.ForPlayer(player, "Plugin.AlertForPlayers"));
        }
    }

    private void OnMapStart(string mapName)
    {
        Server.NextWorldUpdate(() =>
        {
            InitGameRules();
            OnRoundPrestart();
        });
    }
}