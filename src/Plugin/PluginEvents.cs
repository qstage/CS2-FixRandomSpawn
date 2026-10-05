using System.Runtime.InteropServices;
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
        var mp_randomspawn = ConVar.Find("mp_randomspawn");
        var mp_buy_anywhere = ConVar.Find("mp_buy_anywhere");

        if (mp_randomspawn == null || mp_buy_anywhere == null)
            return HookResult.Continue;

        if (!_gameRules.WarmupPeriod && Config.WarmupMode.Enable)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                _memoryPatch.Restore();
            else
                SetUseNormalSpawns(true);

            mp_randomspawn.SetValue(0);
            if (Config.WarmupMode.BuyAnywhere) mp_buy_anywhere.SetValue(0);

            return HookResult.Continue;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Config.WarmupMode.Enable && mp_randomspawn.GetPrimitiveValue<int>() == 0)
        {
            _memoryPatch.Init(GameData.GetSignature("CCSPlayerPawn_EntSelectSpawnPoint"));
            _memoryPatch.Apply(GameData.GetSignature("CCSPlayerPawn_EntSelectSpawnPoint_Patch"));
        }

        mp_randomspawn.SetValue(1);

        if (Config.WarmupMode.BuyAnywhere)
            mp_buy_anywhere.SetValue(1);

        return HookResult.Continue;
    }

    private void OnClientPutInServer(int playerSlot)
    {
        var player = Utilities.GetPlayerFromSlot(playerSlot);
        if (player == null || player.IsBot) return;

        if (_gameRules.WarmupPeriod && Config.WarmupMode.Enable && Config.WarmupMode.AlertForPlayers)
            player.PrintToChat(Localizer.ForPlayer(player, "Plugin.AlertForPlayers"));
    }

    private static void SetUseNormalSpawns(bool value)
    {
        Utilities.FindAllEntitiesByDesignerName<CMapInfo>("info_map_parameters")
            .FirstOrDefault()?
            .UseNormalSpawnsForDM = value;
    }

    private void OnMapStart(string mapName)
    {
        Server.NextWorldUpdate(() =>
        {
            InitGameRules();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                SetUseNormalSpawns(false);

            OnRoundPrestart();
        });
    }
}