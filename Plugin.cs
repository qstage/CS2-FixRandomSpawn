using System.Runtime.InteropServices;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace FixRandomSpawn;

public unsafe class Plugin : BasePlugin
{
    public override string ModuleName => "FixRandomSpawn";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "xstage";

    private readonly List<byte> oldBytes = [];
    private byte* ptr;

    private static MemoryFunctionWithReturn<nint, nint>? MaintainDMSpawnPopulation;
    
    [DllImport("kernel32.dll")]
    private static extern bool VirtualProtect(nint address, uint dwSize, int newProtect, int* oldProtect);

    public override void Load(bool hotReload)
    {
        string signature = GameData.GetSignature("FindAsmInstruction");
        ptr = (byte*)NativeAPI.FindSignature(Addresses.ServerPath, signature);
        int length = signature.Count(c => c == ' ') + 1;
        bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        if (!isLinux)
        {
            int *oldProtect = stackalloc int[1];
            VirtualProtect((nint)ptr, (uint)length, 0x40, oldProtect);
        }
        else
        {
            MaintainDMSpawnPopulation = new(GameData.GetSignature("MaintainDMSpawnPopulation"));
            MaintainDMSpawnPopulation.Hook(Hook_MaintainDMSpawnPopulation, HookMode.Post);
        }

        for (int i = 0; i < length; i++)
        {
            oldBytes.Add(ptr[i]);

            if (isLinux)
            {
                int nextByte = i + 1;

                if (nextByte < length && ptr[i] == 0x0f && ptr[nextByte] == 0x85)
                {
                    oldBytes.Add(ptr[nextByte]);

                    ptr[i] = 0x90;
                    ptr[nextByte] = 0xe9;

                    break;
                }
            }

            ptr[i] = 0x90;
        }
    }

    private HookResult Hook_MaintainDMSpawnPopulation(DynamicHook hook) => HookResult.Continue;

    public override void Unload(bool hotReload)
    {
        for (int i = 0; i < oldBytes.Count; i++)
        {
            ptr[i] = oldBytes[i];
        }

        MaintainDMSpawnPopulation?.Unhook(Hook_MaintainDMSpawnPopulation, HookMode.Post);
    }
}