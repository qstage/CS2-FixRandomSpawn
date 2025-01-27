using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Extensions;
using Microsoft.Extensions.Logging;

namespace FixRandomSpawn;

public class Warmup
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; } = false;

    [JsonPropertyName("buy_anywhere")]
    public bool BuyAnywhere { get; set; } = false;

    [JsonPropertyName("alert_for_players")]
    public bool AlertForPlayers { get; set; } = false; 
}

public class PluginConfig : BasePluginConfig
{
    [JsonPropertyName("warmup_mode")]
    public Warmup WarmupMode { get; set; } = new();
}

public sealed partial class Plugin : IPluginConfig<PluginConfig>
{
    public PluginConfig Config { get; set; } = new();

    [ConsoleCommand("css_randomspawn_reload", $"Reload plugin FixRandomSpawn`")]
    public void OnConfigReload(CCSPlayerController? player, CommandInfo info)
    {
        try
        {
            Config.Reload();

            info.ReplyToCommand($"[{ModuleName}] You have successfully reloaded the config.");
        }
        catch (Exception ex)
        {
            info.ReplyToCommand($"[{ModuleName}] An error occurred.");
            Logger.LogError("An error occurred while reloading the configuration: {error}", ex.Message);
        }
    }

    public void OnConfigParsed(PluginConfig config)
    {
        if (config.Version < Config.Version)
        {
            Logger.LogError("Your plugin configuration version is outdated! (v. {old} -> v. {new})", config.Version, Config.Version);
        }

        Config = config;
    }
}