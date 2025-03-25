using System.Globalization;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;

namespace FixRandomSpawn;

public unsafe partial class MemoryPatch(string? modulePath = null)
{
    [LibraryImport("libc", EntryPoint = "mprotect")]
    private static partial int MProtect(nint address, int len, int protect);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private unsafe static partial bool VirtualProtect(nint address, int dwSize, int newProtect, int* oldProtect);

    private readonly string modulePath_ = modulePath ?? Addresses.ServerPath;
    private readonly Dictionary<int, List<byte>> oldPattern_ = [];
    private nint addr_;

    private enum MemoryAccess
    {
        Read = 1,
        Write,
        Exec = 4
    }

    private const int PAGE_READONLY = 0x2;
    private const int PAGE_READWRITE = 0x4;
    private const int PAGE_EXECUTE_READ = 0x20;
    private const int PAGE_EXECUTE_READWRITE = 0x40;

    private const int PAGESIZE = 4096;

    public void Init(string signature)
    {
        addr_ = NativeAPI.FindSignature(modulePath_, signature);
    }

    public void Apply(string patchSignature, int offset = 0)
    {
        if (string.IsNullOrEmpty(patchSignature) || oldPattern_.ContainsKey(offset)) return;

        byte[] bytes = [.. patchSignature.Split(' ').Select(b => byte.Parse(b, NumberStyles.HexNumber))];
        oldPattern_[offset] = [];

        byte* ptrAddr = GetPtr<byte>(offset);
        SetMemAccess(ptrAddr, bytes.Length, MemoryAccess.Read | MemoryAccess.Write | MemoryAccess.Exec);

        for (int i = 0; i < bytes.Length; i++)
        {
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
            SetMemAccess(ptrAddr, pattern.Count, MemoryAccess.Read | MemoryAccess.Write | MemoryAccess.Exec);

            for (int i = 0; i < pattern.Count; i++)
            {
                ptrAddr[i] = pattern[i];
            }

            oldPattern_.Remove(offset);
        }
    }

    public void Write<T>(T data, int offset = 0) where T: unmanaged
    {
        T* ptrAddr = GetPtr<T>(offset);
        SetMemAccess(ptrAddr, sizeof(T), MemoryAccess.Write | MemoryAccess.Read | MemoryAccess.Exec);

        *ptrAddr = data;
    }

    public T Read<T>(int offset = 0) where T: unmanaged
    {
        T* ptrAddr = GetPtr<T>(offset);
        SetMemAccess(ptrAddr, sizeof(T), MemoryAccess.Read | MemoryAccess.Exec);

        return *ptrAddr;
    }

    private T* GetPtr<T>(int offset = 0) where T: unmanaged
    {
        return (T*)(addr_ + offset);
    }

    private bool SetMemAccess<T>(T* ptrAddr, int size, MemoryAccess access) where T: unmanaged
    {
        nint addr = (nint)ptrAddr;

        if (addr == nint.Zero)
            throw new ArgumentNullException(nameof(ptrAddr));

        nint LALIGN(nint addr) => addr & ~(PAGESIZE-1);
        int LALDIF(nint addr) => (int)(addr % PAGESIZE);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return MProtect(LALIGN(addr), size + LALDIF(addr), (int)access) == 0;
        }

        int* oldProtect = stackalloc int[1];
        int prot = access switch
        {
            MemoryAccess.Read => PAGE_READONLY,
            MemoryAccess.Write => PAGE_READWRITE,
            MemoryAccess.Exec => PAGE_EXECUTE_READ,
            _ => PAGE_EXECUTE_READWRITE
        };

        return VirtualProtect(addr, size, prot, oldProtect);
    }
}