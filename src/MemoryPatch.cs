using System.Globalization;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;

namespace FixRandomSpawn;

using size_t = nuint;

public unsafe partial class MemoryPatch(string? modulePath = null)
{
    [LibraryImport("libc", EntryPoint = "mprotect")]
    private static partial int MProtect(nint addr, size_t len, int protect);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool VirtualProtect(nint addr, size_t dwSize, nuint newProtect, nuint* oldProtect);

    public enum MemoryAccess
    {
        Read = 1 << 0,
        Write = 1 << 1,
        Exec = 1 << 2
    }

    private readonly string _modulePath = modulePath ?? Addresses.ServerPath;
    private readonly Dictionary<int, List<byte>> _oldPattern = [];
    private nint _addr;

    private const int PAGE_READONLY = 0x2;
    private const int PAGE_READWRITE = 0x4;
    private const int PAGE_EXECUTE_READ = 0x20;
    private const int PAGE_EXECUTE_READWRITE = 0x40;
    private const int PAGESIZE = 4096;

    public void Init(string signature)
    {
        _addr = NativeAPI.FindSignature(_modulePath, signature);
    }

    public void Apply(string patchSignature, int offset = 0)
    {
        if (string.IsNullOrEmpty(patchSignature) || _oldPattern.ContainsKey(offset)) return;

        byte[] patchPattern = [.. patchSignature.Split(' ').Select(b => byte.Parse(b, NumberStyles.HexNumber))];
        _oldPattern[offset] = [];

        for (int i = 0; i < patchPattern.Length; i++)
        {
            _oldPattern[offset].Add(Read<byte>(offset + i));
            Write(patchPattern[i], offset + i);
        }
    }

    public void Restore()
    {
        if (_oldPattern.Count == 0) return;

        foreach (var (offset, pattern) in _oldPattern)
        {
            for (int i = 0; i < pattern.Count; i++)
            {
                Write(pattern[i], offset + i);
            }

            _oldPattern.Remove(offset);
        }
    }

    public void Write<T>(T data, int offset = 0) where T: unmanaged
    {
        *GetPtr<T>(offset) = data;
    }

    public T Read<T>(int offset = 0) where T: unmanaged
    {
        return *GetPtr<T>(offset);
    }

    public T* GetPtr<T>(int offset = 0) where T: unmanaged
    {
        nint addr = _addr + offset;
        SetMemAccess(addr, (size_t)sizeof(T));

        return (T*)addr;
    }

    public static bool SetMemAccess(nint addr, size_t size, MemoryAccess access = MemoryAccess.Write | MemoryAccess.Read | MemoryAccess.Exec)
    {
        if (addr == nint.Zero)
        {
            throw new ArgumentNullException(nameof(addr));
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return MProtect(LALIGN(addr), size + LALDIF(addr), (int)access) == 0;
        }

        nuint* oldProtect = stackalloc nuint[1];
        nuint prot = access switch
        {
            MemoryAccess.Read => PAGE_READONLY,
            MemoryAccess.Write => PAGE_READWRITE,
            MemoryAccess.Exec => PAGE_EXECUTE_READ,
            _ => PAGE_EXECUTE_READWRITE
        };

        return VirtualProtect(addr, size, prot, oldProtect);
    }

    private static nuint LALDIF(nint addr) => (nuint)addr % PAGESIZE;

    private static nint LALIGN(nint addr) => addr & ~(PAGESIZE - 1);
}