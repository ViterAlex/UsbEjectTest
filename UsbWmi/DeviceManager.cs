using System;
using System.Collections.Generic;
using System.Threading;
using UsbWmi.WMI;

namespace UsbWmi
{
    public class DeviceManager
    {
        public List<UsbDeviceInfo> AllowedDevices { get; set; }
        private UsbDeviceInfo _udf;
        public event EventHandler<DeviceAddingEventArgs> NewDevice;
        public event Action<UsbDeviceInfo> DeviceAccepted;
        public event Action<UsbDeviceInfo> DeviceRejected;
        private readonly DeviceWatcher _dw;
        //Контекст синхронизации
        private readonly SynchronizationContext _context;
        public DeviceManager(SynchronizationContext context)
        {
            _context = context;
            _dw = new DeviceWatcher(_context);
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
            _context.Send(OnNewDevice, _udf);
        }

        protected void OnNewDevice(object udf)
        {
            if (NewDevice == null) return;
            if (!(udf is UsbDeviceInfo)) return;
            var eventArgs = new DeviceAddingEventArgs
            {
                HashCode = udf.GetHashCode().ToString(),
                Cancel = false
            };
            NewDevice((UsbDeviceInfo)udf, eventArgs);
            if (eventArgs.Cancel)
            {
                WinApi.EjectDrive(((UsbDeviceInfo)udf).VolumeLabel);
                OnDeviceRejected((UsbDeviceInfo)udf);
            }
            else
            {
                OnDeviceAccepted((UsbDeviceInfo)udf);
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
