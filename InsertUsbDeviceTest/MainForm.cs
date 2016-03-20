﻿using System;
using System.Diagnostics;
using System.Windows.Forms;
using WMI;
namespace InsertUsbDeviceTest
{
    public partial class MainForm : Form
    {

        public MainForm()
        {
            InitializeComponent();
            DeviceWatcher dw = new DeviceWatcher(System.Threading.SynchronizationContext.Current);
            UsbDeviceInfo udf = null;
            ejectButton.Enabled = !string.IsNullOrEmpty(txtPID.Text);
            ejectButton.Click += (s, e) =>
            {
                if (udf == null)
                {
                    return;
                }
                bool ejectionResult = WinAPI.EjectDrive(udf.VolumeLabel);
                //udf = null;
                //dw.Restart();
            };
            dw.DeviceInserted += (o) =>
            {
                udf = new UsbDeviceInfo()
                {
                    PnpDeviceID = o.GetPropertyValue("PNPDeviceID").ToString()
                };
            };

            dw.DeviceRemoved += (o) =>
            {
                if (udf != null && udf.PnpDeviceID == o.GetPropertyValue("PNPDeviceID").ToString())
                {
                    udf = null;
                    SetFieldsText(udf);
                }
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
                SetFieldsText(udf);
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
                if (udf != null && udf.VolumeDeviceID == o.GetPropertyValue("DeviceID").ToString())
                {
                    udf = null;
                    SetFieldsText(udf);
                }
            };
            dw.PartitionArrived += (o) =>
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
                if (udf != null && udf.PartitionDeviceID == o.GetPropertyValue("DeviceID").ToString())
                {
                    udf = null;
                    SetFieldsText(udf);
                }
            };
            //Остановка событий при закрытии формы
            FormClosing += (s, e) => { dw.Stop(); };
        }

        private void SetFieldsText(UsbDeviceInfo udf)
        {
            txtPID.Text = udf != null ? udf.PID : string.Empty;
            txtVID.Text = udf != null ? udf.VID : string.Empty;
            txtSize.Text = udf != null ? udf.Size.ToString() : string.Empty;
            txtSerial.Text = udf != null ? udf.SerialNumber : string.Empty;
        }

        private void txtPID_TextChanged(object sender, EventArgs e)
        {
            ejectButton.Enabled = !string.IsNullOrEmpty((sender as TextBox).Text);
        }
    }
}
