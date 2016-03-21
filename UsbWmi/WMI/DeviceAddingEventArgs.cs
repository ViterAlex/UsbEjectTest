using System.ComponentModel;

namespace UsbWmi.WMI
{
    public class DeviceAddingEventArgs : CancelEventArgs
    {
        public string DriveLetter { get; set; }
    }
}
