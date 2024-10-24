using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

namespace FixRandomSpawn;

public class Plugin : BasePlugin
{
    public override string ModuleName => "FixRandomSpawn";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "xstage";

    private CBaseEntity? lastSpawn;
    private ConVar? mp_randomspawn;

    public override void Load(bool hotReload)
    {
        mp_randomspawn = ConVar.Find("mp_randomspawn");

        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
    }

    private CBaseEntity GetRandomSpawn(List<CBaseEntity> spawnList)
    {
        CBaseEntity? spawn = null;

        while ((spawn = spawnList.ElementAtOrDefault(new Random().Next(0, spawnList.Count - 1))) == null || spawn == lastSpawn)
        {}
        lastSpawn = spawn;

        return spawn;
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        int randomType = (mp_randomspawn?.GetPrimitiveValue<int>()).GetValueOrDefault();
        var spawnList = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_deathmatch_spawn").ToList();

        if (spawnList.Count == 0 || randomType == 0)
            return HookResult.Continue;

        var player = @event.Userid;
        var pawn = player?.PlayerPawn.Value;
        var spawn = GetRandomSpawn(spawnList);

        if (player == null || pawn == null || spawn.AbsOrigin == null)
            return HookResult.Continue;

        if (randomType == 1 || randomType == player.TeamNum)
            pawn.Teleport(spawn.AbsOrigin, spawn.AbsRotation);

        return HookResult.Continue;
    }
}
