using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Cvars;

namespace FixRandomSpawn;

public sealed partial class Plugin
{
    [GameEventHandler]
    public HookResult OnRoundPrestart(EventRoundPrestart @event, GameEventInfo info)
    {
        if (!Config.WarmupMode.Enable)
            return HookResult.Continue;

        int value = Convert.ToInt32(gameRules_.Get().WarmupPeriod);

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

    [GameEventHandler]
    public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (player == null || @event.Oldteam != 0 || player.IsBot) return HookResult.Continue;

        if (gameRules_.Get().WarmupPeriod && Config.WarmupMode.Enable && Config.WarmupMode.AlertForPlayers)
        {
            player.PrintToChat(Localizer.ForPlayer(player, "Plugin.AlertForPlayers"));
        }

        return HookResult.Continue;
    }
}