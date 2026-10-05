using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

namespace FixRandomSpawn;

public sealed partial class Plugin : BasePlugin
{
    public override string ModuleName { get; } = "FixRandomSpawn";
    public override string ModuleVersion { get; } = "1.2.0";
    public override string ModuleAuthor { get; } = "xstage";

    private CCSGameRules _gameRules = null!;
    private readonly MemoryPatch _memoryPatch = new();
    
    public override void Load(bool hotReload)
    {
        _memoryPatch.Init(GameData.GetSignature("CCSPlayerPawn_EntSelectSpawnPoint"));
        _memoryPatch.Apply(GameData.GetSignature("CCSPlayerPawn_EntSelectSpawnPoint_Patch"));

        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServer);
        RegisterEventHandler<EventRoundPrestart>(OnRoundPrestart);

        if (hotReload) 
            InitGameRules();
    }

    public override void Unload(bool hotReload)
    {
        _memoryPatch.Restore();
    }

    private void InitGameRules()
    {
        try
        {
            _gameRules = FindGameRules();
        }
        catch (Exception ex)
        {
            Logger.LogError("{errorMsg}", ex.Message);
        }
    }

    private static CCSGameRules FindGameRules()
    {
        var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault()?.GameRules;
        return gameRules ?? throw new Exception("Not found CCSGameRules");
    }
}