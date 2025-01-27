using System.Globalization;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;

namespace FixRandomSpawn;

public unsafe partial class MemoryPatch
{
    [LibraryImport("libc", EntryPoint = "mprotect")]
    private static partial int MProtect(nint address, int len, int protect);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private unsafe static partial bool VirtualProtect(nint address, int dwSize, int newProtect, int* oldProtect);

    private readonly Dictionary<int, List<byte>> oldPattern_ = [];
    private readonly string modulePath_;
    private nint addr_;

    public MemoryPatch(string? modulePath = null)
    {
        modulePath_ = modulePath ?? Addresses.ServerPath;
    }

    public void Init(string signature)
    {
        addr_ = NativeAPI.FindSignature(modulePath_, signature);
    }

    public void Apply(string patchSignature, int offset = 0)
    {
        if (string.IsNullOrEmpty(patchSignature) || oldPattern_.ContainsKey(offset)) return;

        byte[] bytes = [.. patchSignature.Split(' ').Select(b => byte.Parse(b, NumberStyles.HexNumber))];
        byte* ptrAddr = GetPtr<byte>(offset);

        SetMemAccess(ptrAddr, bytes.Length);

        for (int i = 0; i < bytes.Length; i++)
        {
            oldPattern_[offset] = [];
            oldPattern_[offset].Add(ptrAddr[i]);
            
            ptrAddr[i] = bytes[i];
        }
    }

    public void Restore()
    {
        if (oldPattern_.Count == 0) return;

        foreach (var (offset, pattern) in oldPattern_)
        {
            byte* ptrAddr = GetPtr<byte>(offset);

            for (int i = 0; i < pattern.Count; i++)
            {
                ptrAddr[i] = pattern[i];
            }
        }
    }

    private T* GetPtr<T>(int offset = 0) where T: unmanaged => (T*)(addr_ + offset);

    private bool SetMemAccess<T>(T* ptrAddr, int size) where T: unmanaged
    {
        nint addr = (nint)ptrAddr;

        if (addr == nint.Zero)
            throw new ArgumentNullException(nameof(ptrAddr));

        const int PAGESIZE = 4096;

        nint LALIGN(nint addr) => addr & ~(PAGESIZE-1);
        int LALDIF(nint addr) => (int)(addr % PAGESIZE);

        int* oldProtect = stackalloc int[1];

        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? 
            MProtect(LALIGN(addr), size + LALDIF(addr), 7) == 0 : VirtualProtect(addr, size, 0x40, oldProtect);
    }
} 