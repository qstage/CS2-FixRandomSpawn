using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;

namespace FixRandomSpawn;

[MinimumApiVersion(305)]
public sealed partial class Plugin : BasePlugin
{
    public override string ModuleName { get; } = "FixRandomSpawn";
    public override string ModuleVersion { get; } = "1.1.2";
    public override string ModuleAuthor { get; } = "xstage";

    private readonly GameRules gameRules_ = new GameRules();
    private readonly MemoryPatch memoryPatch_ = new MemoryPatch();
    
    public override void Load(bool hotReload)
    {
        gameRules_.Init(hotReload, this);

        memoryPatch_.Init(GameData.GetSignature("EntSelectSpawnPoint"));
        memoryPatch_.Apply(GameData.GetSignature("EntSelectSpawnPoint_Patch1"), GameData.GetOffset("EntSelectSpawnPoint_Patch1"));
    }

    public override void Unload(bool hotReload)
    {
        memoryPatch_.Restore();
    }
}