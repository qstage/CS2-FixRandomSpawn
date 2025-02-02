using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

namespace FixRandomSpawn;

public sealed class GameRules
{
    private Plugin plugin_ = null!;
    private CCSGameRules value_ = null!;

    public void Init(bool hotReload, Plugin plugin)
    {
        plugin_ = plugin;

        plugin.RegisterListener<Listeners.OnMapStart>(manName =>
        {
            Server.NextWorldUpdate(() =>
            {
                FindGameRules();
            });
        });
        if (hotReload) FindGameRules();
    }

    public CCSGameRules Get() => value_;

    public void FindGameRules()
    {
        try
        {
            value_ = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;
        }
        catch (Exception)
        {
            plugin_.Logger.LogError("Couldn't find `CCSGameRules`");
        }
    }
}