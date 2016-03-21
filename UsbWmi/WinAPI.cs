using System;
using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming

namespace UsbWmi
{
    public static class WinApi
    {
        private const int OPEN_EXISTING = 3;
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 1;
        private const uint FILE_SHARE_WRITE = 2;
        private const uint FSCTL_LOCK_VOLUME = 0x00090018;
        private const uint FSCTL_DISMOUNT_VOLUME = 0x00090020;
        private const uint FSCTL_UNLOCK_VOLUME = 0x0009001c;
        private const uint IOCTL_STORAGE_EJECT_MEDIA = 0x2D4808;
        private const uint IOCTL_STORAGE_MEDIA_REMOVAL = 0x002D4804;

        [StructLayout(LayoutKind.Sequential)]
        private struct PREVENT_MEDIA_REMOVAL
        {
            public bool PreventMediaRemoval;
        }
        [DllImport("kernel32")]
        private static extern int CloseHandle(IntPtr handle);

        [DllImport("kernel32")]
        private static extern bool DeviceIoControl
            (IntPtr deviceHandle, uint ioControlCode,
              IntPtr inBuffer, int inBufferSize,
              IntPtr outBuffer, int outBufferSize,
              ref int bytesReturned, IntPtr overlapped);

        [DllImport("kernel32")]
        private static extern IntPtr CreateFile
            (string filename, uint desiredAccess,
              uint shareMode, IntPtr securityAttributes,
              int creationDisposition, int flagsAndAttributes,
              IntPtr templateFile);

        /// <summary>
        /// Извлечение диска
        /// </summary>
        /// <param name="letter">Буква диска, который нужно извлечь</param>
        public static bool EjectDrive(string letter)
        {
            var handle = OpenVolume(letter);
            if (handle.ToInt32() == -1)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                return false;
            }

            if (!LockVolume(handle))
            {
                return false;
            }
            if (!DismountVolume(handle))
            {
                return false;
            }
            if (!PreventRemovalOfVolume(handle, false))
            {
                return false;
            }
            if (!EjectVolume(handle))
            {
                return false;
            }
            CloseHandle(handle);
            return true;
        }

        private static IntPtr OpenVolume(string driveLetter)
        {
            var path = string.Format("\\\\.\\{0}", driveLetter);
            return CreateFile(path, GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
        }

        private static bool LockVolume(IntPtr handle)
        {
            var dwBytesReturned = 0;
            return DeviceIoControl(handle, FSCTL_LOCK_VOLUME, IntPtr.Zero, 0,
                IntPtr.Zero, 0, ref dwBytesReturned, IntPtr.Zero);
        }

        private static bool DismountVolume(IntPtr handle)
        {
            var dwBytesReturned = 0;
            return DeviceIoControl(handle, FSCTL_DISMOUNT_VOLUME, IntPtr.Zero, 0,
                IntPtr.Zero, 0, ref dwBytesReturned, IntPtr.Zero);
        }

        private static bool PreventRemovalOfVolume(IntPtr handle, bool preventRemoval)
        {
            var dwBytesReturned = 0;
            PREVENT_MEDIA_REMOVAL pmrBuffer;
            pmrBuffer.PreventMediaRemoval = preventRemoval;
            var hPmrBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(pmrBuffer));
            Marshal.StructureToPtr(pmrBuffer, hPmrBuffer, false);
            return DeviceIoControl(handle, IOCTL_STORAGE_MEDIA_REMOVAL, hPmrBuffer, Marshal.SizeOf(pmrBuffer),
                IntPtr.Zero, 0, ref dwBytesReturned, IntPtr.Zero);
        }

        private static bool EjectVolume(IntPtr handle)
        {
            var dwBytesReturned = 0;
            return DeviceIoControl(handle, IOCTL_STORAGE_EJECT_MEDIA, IntPtr.Zero, 0,
                IntPtr.Zero, 0, ref dwBytesReturned, IntPtr.Zero);
        }
    }
}
