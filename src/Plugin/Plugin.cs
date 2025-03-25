using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace FixRandomSpawn;

[MinimumApiVersion(305)]
public sealed partial class Plugin : BasePlugin
{
    public override string ModuleName { get; } = "FixRandomSpawn";
    public override string ModuleVersion { get; } = "1.1.3";
    public override string ModuleAuthor { get; } = "xstage";

    private CCSGameRules _gameRules = null!;
    private readonly MemoryPatch _memoryPatch = new();
    
    public override void Load(bool hotReload)
    {
        _memoryPatch.Init(GameData.GetSignature("EntSelectSpawnPoint"));
        _memoryPatch.Apply(GameData.GetSignature("EntSelectSpawnPoint_Patch1"), GameData.GetOffset("EntSelectSpawnPoint_Patch1"));

        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServer);
        RegisterEventHandler<EventRoundPrestart>(OnRoundPrestart);

        if (hotReload) 
        {
            InitGameRules();
        }
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