using System;
using System.Collections.Generic;
using UsbWmi.WMI;

namespace UsbWmi
{
    public class DeviceManager
    {
        public List<UsbDeviceInfo> AllowedDevices { get; set; }
        private UsbDeviceInfo _udf;
        private readonly DeviceWatcher _dw = new DeviceWatcher();
        public event Action<UsbDeviceInfo> DeviceAccepted;
        public event Action<UsbDeviceInfo> DeviceRejected;
        public DeviceManager()
        {
            _dw.DeviceInserted += _dw_DeviceInserted;
            _dw.DeviceRemoved += _dw_DeviceRemoved;
            _dw.DiskDriveInserted += _dw_DiskDriveInserted;
            _dw.VolumeMounted += _dw_VolumeMounted;
            _dw.VolumeDismounted += _dw_VolumeDismounted;
            _dw.PartitionCreated += _dw_PartitionCreated;
            _dw.PartitionRemoved += _dw_PartitionRemoved;
            _dw.AllCreateEventsFired += DwAllCreateEventsFired;
        }

        private void DwAllCreateEventsFired()
        {

            if (AllowedDevices.Contains(_udf))
                OnDeviceAccepted(_udf);
            else
            {
                WinApi.EjectDrive(_udf.VolumeLabel);
                OnDeviceRejected(_udf);
            }
        }

        protected void OnDeviceRejected(UsbDeviceInfo deviceInfo)
        {
            if (DeviceRejected == null) return;
            DeviceRejected(deviceInfo);
        }

        protected void OnDeviceAccepted(UsbDeviceInfo deviceInfo)
        {
            if (DeviceAccepted == null) return;
            DeviceAccepted(deviceInfo);
        }

        public void Stop()
        {
            _dw.Stop();
        }

        private void _dw_PartitionRemoved(System.Management.ManagementBaseObject obj)
        {
            if (_udf == null || _udf.PartitionDeviceID != obj.GetPropertyValue("DeviceId").ToString()) return;
            _udf = null;
        }

        private void _dw_PartitionCreated(System.Management.ManagementBaseObject obj)
        {
            //Ждём пока выстрелит событие DeviceInserted
            while (_udf == null)
            {
                System.Threading.Thread.Sleep(100);
            }
            _udf.PartitionDeviceID = obj.GetPropertyValue("DeviceId").ToString();
        }

        private void _dw_VolumeDismounted(System.Management.ManagementBaseObject obj)
        {
            if (_udf == null || _udf.VolumeDeviceID != obj.GetPropertyValue("DeviceId").ToString()) return;
            _udf = null;
        }

        private void _dw_VolumeMounted(System.Management.ManagementBaseObject obj)
        {
            //Ждём пока выстрелит событие DeviceInserted
            while (_udf == null)
            {
                System.Threading.Thread.Sleep(100);
            }
            _udf.VolumeDeviceID = obj.GetPropertyValue("DeviceId").ToString();
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
            if (_udf == null || _udf.PnpDeviceId != obj.GetPropertyValue("PNPDeviceID").ToString()) return;
            _udf = null;
        }

        private void _dw_DeviceInserted(System.Management.ManagementBaseObject obj)
        {
            _udf = new UsbDeviceInfo()
            {
                PnpDeviceId = obj.GetPropertyValue("PNPDeviceID").ToString()
            };
        }
    }
}
