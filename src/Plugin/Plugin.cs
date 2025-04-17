using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;

namespace FixRandomSpawn;

[MinimumApiVersion(307)]
public sealed partial class Plugin : BasePlugin
{
    public override string ModuleName { get; } = "FixRandomSpawn";
    public override string ModuleVersion { get; } = "1.1.4";
    public override string ModuleAuthor { get; } = "xstage";

    private readonly Func<CCSGameRules> CSGameRules = CreateGameRulesGetter();
    private readonly MemoryPatch _memoryPatch = new();
    
    public override void Load(bool hotReload)
    {
        _memoryPatch.Init(GameData.GetSignature("EntSelectSpawnPoint"));
        _memoryPatch.Apply(GameData.GetSignature("EntSelectSpawnPoint_Patch1"), GameData.GetOffset("EntSelectSpawnPoint_Patch1"));
    }

    public override void Unload(bool hotReload) => _memoryPatch.Restore();

    private static Func<CCSGameRules> CreateGameRulesGetter()
    {
        CCSGameRulesProxy? cCSGameRulesProxy = null;

        CCSGameRules GetCSGameRules()
        {
            if ( cCSGameRulesProxy != null && cCSGameRulesProxy.IsValid && cCSGameRulesProxy.GameRules != null )
            {
                return cCSGameRulesProxy.GameRules;
            }

            cCSGameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First();

            return GetCSGameRules();
        }

        return GetCSGameRules;
    }
}