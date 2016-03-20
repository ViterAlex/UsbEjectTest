using System.Collections.Generic;
using System.Threading;
using WMI;

namespace InsertUsbDeviceTest.WMI
{
    public class DeviceManager
    {
        public List<UsbDeviceInfo> AllowedDevices { get; set; }

        public DeviceManager(SynchronizationContext context)
        {
            var dw = new DeviceWatcher(context);
            UsbDeviceInfo udf = null;
            dw.DeviceInserted += (o) =>
            {
                udf = new UsbDeviceInfo()
                {
                    PnpDeviceID = o.GetPropertyValue("PNPDeviceID").ToString()
                };
            };

            dw.DeviceRemoved += (o) =>
            {
                if (udf == null || udf.PnpDeviceID != o.GetPropertyValue("PNPDeviceID").ToString()) return;
                udf = null;
            };

            dw.DiskDriveInserted += (o) =>
            {
                //Ждём пока выстрелит событие DeviceInserted
                while (udf == null)
                {
                    System.Threading.Thread.Sleep(100);
                }
                udf.Size = (ulong)o.GetPropertyValue("Size");
                udf.SerialNumber = o.GetPropertyValue("SerialNumber").ToString();
            };

            dw.VolumeMounted += (o) =>
            {
                //Ждём пока выстрелит событие DeviceInserted
                while (udf == null)
                {
                    System.Threading.Thread.Sleep(100);
                }
                udf.VolumeDeviceID = o.GetPropertyValue("DeviceID").ToString();
                udf.VolumeLabel = o.GetPropertyValue("Caption").ToString().Substring(0, 2);
            };

            dw.VolumeDismounted += (o) =>
            {
                if (udf == null || udf.VolumeDeviceID != o.GetPropertyValue("DeviceID").ToString()) return;
                udf = null;
            };
            dw.PartitionCreated += (o) =>
            {
                //Ждём пока выстрелит событие DeviceInserted
                while (udf == null)
                {
                    System.Threading.Thread.Sleep(100);
                }
                udf.PartitionDeviceID = o.GetPropertyValue("DeviceID").ToString();

            };

            dw.PartitionRemoved += (o) =>
            {
                if (udf == null || udf.PartitionDeviceID != o.GetPropertyValue("DeviceID").ToString()) return;
                udf = null;
            };
            dw.DeviceAdded += (o, e) =>
            {
                if (udf == null || AllowedDevices?.Count == 0) return;
                e.Cancel = AllowedDevices.Contains(udf);
            };
        }
    }
}
