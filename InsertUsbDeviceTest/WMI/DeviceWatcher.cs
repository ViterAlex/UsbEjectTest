﻿using System;
using System.Collections.Generic;
using System.Management;
using System.Threading;

namespace InsertUsbDeviceTest.WMI
{
    public class DeviceWatcher
    {

        //Поле для синхронизации между потоком формы и событий WMI
        private readonly SynchronizationContext _context;
        //Отслеживание создания классов WMI
        private List<ManagementEventWatcher> _createWatchers;
        //Отслеживание удаления классов WMI
        private List<ManagementEventWatcher> _removeWatchers;
        //Обработчики создания классов WMI
        private List<EventArrivedEventHandler> _createHandlers;
        //Обработчики удаления классов WMI
        private List<EventArrivedEventHandler> _removeHandlers;

        private int _removeEventsCounter;
        private int RemoveEventsCounter
        {
            get { return _removeEventsCounter; }
            set
            {
                _removeEventsCounter = value;
                if (_removeEventsCounter == 0)
                {
                    Restart();
                }
            }
        }

        private int CreateEventsCounter { get; set; }

        public DeviceWatcher(SynchronizationContext context)
            :this()
        {
            _context = context;
        }

        public DeviceWatcher()
        {
            _context = new SynchronizationContext();
            AddHandlers();
            AddWatchers();
            Restart();
        }
        private void AddHandlers()
        {
            _removeHandlers = new List<EventArrivedEventHandler>() {
                DeviceRemovedEvent,
                VolumeDismountedEvent,
                PartitionRemoveEvent
            };
            _createHandlers = new List<EventArrivedEventHandler>() {
                DeviceInsertedEvent,
                DiskDriveInsertedEvent,
                VolumeMountedEvent,
                PartitionArriveEvent,
            };
        }

        internal void Restart()
        {
            Stop();
            _createWatchers.ForEach(w => w.Start());
            _removeWatchers.ForEach(w => w.Start());
            _removeEventsCounter = _removeHandlers.Count;
            CreateEventsCounter = _createHandlers.Count;
        }

        private void AddWatchers()
        {
            _removeWatchers = new List<ManagementEventWatcher>(){
                new ManagementEventWatcher("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'"),
                new ManagementEventWatcher("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_Volume'"),
                new ManagementEventWatcher("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_DiskPartition'")
            };
            _createWatchers = new List<ManagementEventWatcher>() {
                new ManagementEventWatcher("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'"),
                new ManagementEventWatcher("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_DiskDrive'"),
                new ManagementEventWatcher("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_Volume'"),
                new ManagementEventWatcher("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_DiskPartition'")
            };
            _createWatchers.ForEach(w => w.EventArrived += _createHandlers[_createWatchers.IndexOf(w)]);
            _removeWatchers.ForEach(w => w.EventArrived += _removeHandlers[_removeWatchers.IndexOf(w)]);
        }

        internal void Stop()
        {
            _createWatchers.ForEach(w => w.Stop());
            _removeWatchers.ForEach(w => w.Stop());
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
            if (DeviceInserted == null) return;
            DeviceInserted((ManagementBaseObject)o);
            CreateEventsCounter++;
        }
        //Обёртка над событием DeviceRemoved
        protected void OnDeviceRemoved(object o)
        {
            if (DeviceRemoved == null) return;
            DeviceRemoved((ManagementBaseObject)o);
            RemoveEventsCounter--;
        }
        //Обёртка над событием DiskDriveInserted
        protected void OnDiskDriveInserted(object o)
        {
            if (DiskDriveInserted == null) return;
            DiskDriveInserted((ManagementBaseObject)o);
            CreateEventsCounter++;
        }
        //Обёртка над событием VolumeMounted
        protected void OnVolumeMounted(object o)
        {
            if (VolumeMounted == null) return;
            var obj = (ManagementBaseObject)o;
            VolumeMounted(obj);
            CreateEventsCounter++;
            //После монтирования тома, можно узнать букву диска и извлечь его при необходимости
            OnDeviceAdded(o);
        }
        //Обёртка над событием VolumeDismounted
        protected void OnVolumeDismounted(object o)
        {
            if (VolumeDismounted == null) return;
            VolumeDismounted((ManagementBaseObject)o);
            RemoveEventsCounter--;
        }
        //Обёртка над событием PartitionCreated
        protected void OnPartitionArrived(object o)
        {
            if (PartitionCreated == null) return;
            PartitionCreated((ManagementBaseObject)o);
            CreateEventsCounter++;
        }
        //Обёртка над событием PartitionRemoved
        protected void OnPartitionRemoved(object o)
        {
            if (PartitionRemoved == null) return;
            PartitionRemoved((ManagementBaseObject)o);
            RemoveEventsCounter--;
        }
        //Обёртка над событием DeviceAdded
        protected void OnDeviceAdded(object o)
        {
            if (DeviceAdded == null) return;
            var obj = (ManagementBaseObject)o;
            if (obj == null) return;
            var e = new DeviceAddingEventArgs() { DriveLetter = obj.GetPropertyValue("Caption").ToString() };
            DeviceAdded(obj, e);
            if (!e.Cancel) return;
            WinApi.EjectDrive(e.DriveLetter);
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
        /// <summary>
        /// Событие, возникающее при создании раздела в системе 
        /// </summary>
        public event Action<ManagementBaseObject> PartitionCreated;
        /// <summary>
        /// Событие, возникающее при удалении раздела в системе 
        /// </summary>
        public event Action<ManagementBaseObject> PartitionRemoved;

        public event EventHandler<DeviceAddingEventArgs> DeviceAdded;

        #endregion
    }
}
