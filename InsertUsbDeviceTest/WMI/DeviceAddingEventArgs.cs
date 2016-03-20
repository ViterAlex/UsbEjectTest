using System.ComponentModel;

namespace InsertUsbDeviceTest.WMI
{
    public class DeviceAddingEventArgs : CancelEventArgs
    {
        public string DriveLetter { get; set; }
    }
}
