using System.Collections.Generic;
using UsbWmi.WMI;

namespace UsbWmi
{
    public class DeviceManager
    {
        public List<UsbDeviceInfo> AllowedDevices { get; set; }
        private UsbDeviceInfo _udf;
        private readonly DeviceWatcher _dw = new DeviceWatcher();

        public DeviceManager()
        {
            _dw.DeviceInserted += _dw_DeviceInserted;
            _dw.DeviceRemoved += _dw_DeviceRemoved;
            _dw.DiskDriveInserted += _dw_DiskDriveInserted;
            _dw.VolumeMounted += _dw_VolumeMounted;
            _dw.VolumeDismounted += _dw_VolumeDismounted;
            _dw.PartitionCreated += _dw_PartitionCreated;
            _dw.PartitionRemoved += _dw_PartitionRemoved;
            _dw.DeviceAdded += _dw_DeviceAdded;
        }

        public void Stop()
        {
            _dw.Stop();
        }

        private void _dw_DeviceAdded(object sender, DeviceAddingEventArgs e)
        {
            if (_udf == null || AllowedDevices?.Count == 0) return;
            if (AllowedDevices != null) e.Cancel = AllowedDevices.Contains(_udf);
        }

        private void _dw_PartitionRemoved(System.Management.ManagementBaseObject obj)
        {
            if (_udf == null || _udf.PartitionDeviceID != obj.GetPropertyValue("DeviceID").ToString()) return;
            _udf = null;
        }

        private void _dw_PartitionCreated(System.Management.ManagementBaseObject obj)
        {
            //Ждём пока выстрелит событие DeviceInserted
            while (_udf == null)
            {
                System.Threading.Thread.Sleep(100);
            }
            _udf.PartitionDeviceID = obj.GetPropertyValue("DeviceID").ToString();
        }

        private void _dw_VolumeDismounted(System.Management.ManagementBaseObject obj)
        {
            if (_udf == null || _udf.VolumeDeviceID != obj.GetPropertyValue("DeviceID").ToString()) return;
            _udf = null;
        }

        private void _dw_VolumeMounted(System.Management.ManagementBaseObject obj)
        {
            //Ждём пока выстрелит событие DeviceInserted
            while (_udf == null)
            {
                System.Threading.Thread.Sleep(100);
            }
            _udf.VolumeDeviceID = obj.GetPropertyValue("DeviceID").ToString();
            _udf.VolumeLabel = obj.GetPropertyValue("Caption").ToString().Substring(0, 2);
        }

        private void _dw_DiskDriveInserted(System.Management.ManagementBaseObject obj)
        {
            //Ждём пока выстрелит событие DeviceInserted
            while (_udf == null)
            {
                System.Threading.Thread.Sleep(100);
            }
            _udf.Size = (ulong)obj.GetPropertyValue("Size");
            _udf.SerialNumber = obj.GetPropertyValue("SerialNumber").ToString();
        }

        private void _dw_DeviceRemoved(System.Management.ManagementBaseObject obj)
        {
            if (_udf == null || _udf.PnpDeviceID != obj.GetPropertyValue("PNPDeviceID").ToString()) return;
            _udf = null;
        }

        private void _dw_DeviceInserted(System.Management.ManagementBaseObject obj)
        {
            _udf = new UsbDeviceInfo()
            {
                PnpDeviceID = obj.GetPropertyValue("PNPDeviceID").ToString()
            };
        }
    }
}
