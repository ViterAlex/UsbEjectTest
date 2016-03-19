using System;
using System.Collections.Generic;
using System.Management;
using System.Threading;

namespace WMI
{
    public class DeviceWatcher
    {
        //Поле для синхронизации между потоком формы и событий WMI
        private SynchronizationContext _context;

        private List<ManagementEventWatcher> _watchers;
        private List<EventArrivedEventHandler> _handlers;

        public DeviceWatcher(SynchronizationContext context)
        {
            AddHandlers();
            AddWatchers();
            _watchers.ForEach((w) => w.Start());
            _context = context;
        }

        private void AddHandlers()
        {
            _handlers = new List<EventArrivedEventHandler>();
            _handlers.AddRange(new EventArrivedEventHandler[] {
                new EventArrivedEventHandler(DeviceInsertedEvent),
                new EventArrivedEventHandler(DeviceRemovedEvent),
                new EventArrivedEventHandler(DiskDriveInsertedEvent),
                new EventArrivedEventHandler(VolumeMountedEvent),
                new EventArrivedEventHandler(VolumeDismountedEvent),
                new EventArrivedEventHandler(PartitionArriveEvent),
                new EventArrivedEventHandler(PartitionRemoveEvent)
            });
        }

        private void AddWatchers()
        {
            _watchers = new List<ManagementEventWatcher>();
            _watchers.AddRange(new ManagementEventWatcher[] {
                new ManagementEventWatcher("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'"),
                new ManagementEventWatcher("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'"),
                new ManagementEventWatcher("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_DiskDrive'"),
                new ManagementEventWatcher("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_Volume'"),
                new ManagementEventWatcher("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_Volume'"),
                new ManagementEventWatcher("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_DiskPartition'"),
                new ManagementEventWatcher("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_DiskPartition'")
            });
            _watchers.ForEach(w => w.EventArrived += _handlers[_watchers.IndexOf(w)]);
        }

        internal void Stop()
        {
            _watchers.ForEach(w => w.Stop());
        }

        #region SynchronizationContext Callers
        private void VolumeMountedEvent(object sender, EventArrivedEventArgs e)
        {
            _context.Send(OnVolumeMounted, e.NewEvent["TargetInstance"]);
        }

        private void VolumeDismountedEvent(object sender, EventArrivedEventArgs e)
        {
            _context.Send(OnVolumeDismounted, e.NewEvent["TargetInstance"]);
        }

        private void DeviceInsertedEvent(object sender, EventArrivedEventArgs e)
        {
            _context.Send(OnDeviceInserted, e.NewEvent["TargetInstance"]);
        }

        private void DeviceRemovedEvent(object sender, EventArrivedEventArgs e)
        {
            _context.Send(OnDeviceRemoved, e.NewEvent["TargetInstance"]);
        }

        private void DiskDriveInsertedEvent(object sender, EventArrivedEventArgs e)
        {
            _context.Send(OnDiskDriveInserted, e.NewEvent["TargetInstance"]);
        }

        private void PartitionArriveEvent(object sender, EventArrivedEventArgs e)
        {
            _context.Send(OnPartitionArrived, e.NewEvent["TargetInstance"]);
        }

        private void PartitionRemoveEvent(object sender, EventArrivedEventArgs e)
        {
            _context.Send(OnPartitionRemoved, e.NewEvent["TargetInstance"]);
        }
        #endregion

        #region EventWrappers
        //Обёртка над событием DeviceInserted
        protected void OnDeviceInserted(object o)
        {
            if (DeviceInserted != null)
            {
                DeviceInserted((ManagementBaseObject)o);
            }
        }
        //Обёртка над событием DeviceRemoved
        protected void OnDeviceRemoved(object o)
        {
            if (DeviceRemoved != null)
            {
                DeviceRemoved((ManagementBaseObject)o);
            }
        }
        //Обёртка над событием DiskDriveInserted
        protected void OnDiskDriveInserted(object o)
        {
            if (DiskDriveInserted != null)
            {
                DiskDriveInserted((ManagementBaseObject)o);
            }
        }
        //Обёртка над событием VolumeMounted
        protected void OnVolumeMounted(object o)
        {
            if (VolumeMounted != null)
            {
                VolumeMounted((ManagementBaseObject)o);
            }
        }
        //Обёртка над событием VolumeDismounted
        protected void OnVolumeDismounted(object o)
        {
            if (VolumeDismounted != null)
            {
                VolumeDismounted((ManagementBaseObject)o);
            }
        }
        //Обёртка над событием PartitionArrived
        protected void OnPartitionArrived(object o)
        {
            if (PartitionArrived != null)
            {
                PartitionArrived((ManagementBaseObject)o);
            }
        }
        //Обёртка над событием PartitionRemoved
        protected void OnPartitionRemoved(object o)
        {
            if (PartitionRemoved != null)
            {
                PartitionRemoved((ManagementBaseObject)o);
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Событие, возникающее при вставке флешки
        /// </summary>
        public event Action<ManagementBaseObject> DeviceInserted;
        /// <summary>
        /// Событие, возникающее при удалении флешки
        /// </summary>
        public event Action<ManagementBaseObject> DeviceRemoved;
        /// <summary>
        /// Событие, возникающее при появлении в системе диска
        /// </summary>
        public event Action<ManagementBaseObject> DiskDriveInserted;
        /// <summary>
        /// Событие, возникающее при появлении в системе нового тома
        /// </summary>
        public event Action<ManagementBaseObject> VolumeMounted;
        /// <summary>
        /// Событие, возникающее при отключении тома в системе 
        /// </summary>
        public event Action<ManagementBaseObject> VolumeDismounted;
        public event Action<ManagementBaseObject> PartitionArrived;
        public event Action<ManagementBaseObject> PartitionRemoved; 
        #endregion
    }
}
