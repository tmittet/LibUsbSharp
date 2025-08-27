using System.Runtime.InteropServices;

namespace LibUsbSharp.Tests.Windows;

public class Handles
{
    // ----- ntdll / kernel32 -----
    private enum SYSTEM_INFORMATION_CLASS : int
    {
        SystemHandleInformation = 16,
    }

    private enum OBJECT_INFORMATION_CLASS : int
    {
        ObjectBasicInformation = 0,
        ObjectNameInformation = 1,
        ObjectTypeInformation = 2,
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_HANDLE
    {
        public ushort ProcessId;
        public ushort CreatorBackTraceIndex;
        public byte ObjectTypeNumber;
        public byte Flags;
        public ushort Handle;
        public IntPtr Object;
        public uint GrantedAccess;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct UNICODE_STRING
    {
        public ushort Length;
        public ushort MaximumLength;
        public IntPtr Buffer; // PWSTR
    }

    [DllImport("ntdll.dll")]
    private static extern int NtQuerySystemInformation(
        SYSTEM_INFORMATION_CLASS SystemInformationClass,
        IntPtr SystemInformation,
        int SystemInformationLength,
        ref int ReturnLength
    );

    [DllImport("ntdll.dll")]
    private static extern int NtQueryObject(
        IntPtr Handle,
        OBJECT_INFORMATION_CLASS ObjectInformationClass,
        IntPtr ObjectInformation,
        int ObjectInformationLength,
        ref int ReturnLength
    );

    [DllImport("kernel32.dll")]
    private static extern int GetCurrentProcessId();

    //[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    //private static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);

    const int STATUS_INFO_LENGTH_MISMATCH = unchecked((int)0xC0000004);

    public static uint GetHandleCount()
    {
        int myPid = GetCurrentProcessId();

        Console.WriteLine($"IM AT PID {myPid} - Getting handle count...");

        IntPtr buf = IntPtr.Zero;
        int size = 0x10000,
            ret = 0;
        uint owned = 0,
            named = 0;

        try
        {
            buf = Marshal.AllocHGlobal(size);
            int status;
            while (
                (
                    status = NtQuerySystemInformation(
                        SYSTEM_INFORMATION_CLASS.SystemHandleInformation,
                        buf,
                        size,
                        ref ret
                    )
                ) == STATUS_INFO_LENGTH_MISMATCH
            )
            {
                size = Math.Max(size * 2, ret);
                Marshal.FreeHGlobal(buf);
                buf = Marshal.AllocHGlobal(size);
            }
            if (status != 0)
            {
                Console.WriteLine($"NtQuerySystemInformation failed: 0x{status:X}");
                return 0;
            }

            int handleCount = Marshal.ReadInt32(buf);
            IntPtr entry = IntPtr.Add(buf, 8);
            int entrySize = Marshal.SizeOf<SYSTEM_HANDLE>();

            for (int i = 0; i < handleCount; i++)
            {
                var sh = Marshal.PtrToStructure<SYSTEM_HANDLE>(entry);
                //Console.WriteLine($"PID: {sh.ProcessId} | Handle: 0x{sh.Handle:X4} | Type: {sh.ObjectTypeNumber} | Flags: {sh.Flags} | Access: 0x{sh.GrantedAccess:X8}");
                if (sh.ProcessId == myPid)
                {
                    owned++;
                    /*
                                        IntPtr h = new IntPtr(sh.Handle);

                                        string typeName = QueryObjectUnicodeString(h, OBJECT_INFORMATION_CLASS.ObjectTypeInformation);
                                        string ntName = QueryObjectUnicodeString(h, OBJECT_INFORMATION_CLASS.ObjectNameInformation);
                                        string pretty = ToDosPathIfPossible(ntName, ntToDos);

                                        if (!string.IsNullOrEmpty(typeName) || !string.IsNullOrEmpty(pretty))
                                            named++;

                                        Console.WriteLine($"Handle 0x{sh.Handle:X4} | Type={typeName ?? "?"} | Name={(pretty ?? ntName ?? "(no name)")}");
                    */
                }

                entry = IntPtr.Add(entry, entrySize);
            }

            Console.WriteLine(
                $"\nTotal handles (system): {handleCount} Total handles (current process): {owned}.  Resolved names/types for ~{named} of them."
            );
        }
        finally
        {
            if (buf != IntPtr.Zero)
                Marshal.FreeHGlobal(buf);
        }

        return owned;
        // Add the missing closing brace for the Handles class
    }
}
