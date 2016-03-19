namespace WMI
{
    public class UsbDeviceInfo
    {

        public string DeviceID { get; set; }
        private string pnpDeviceID;

        public string PnpDeviceID
        {
            get { return pnpDeviceID; }
            set
            {
                pnpDeviceID = value;
                VID = pnpDeviceID.Split('\\')[1].Split('&')[0].Substring(4);
                PID = pnpDeviceID.Split('\\')[1].Split('&')[1].Substring(4);
            }
        }

        public string Description { get; set; }
        public ulong Size { get; set; }
        public string PID { get; set; }
        public string VID { get; set; }
        public string VolumeDeviceID { get; set; }
        public string SerialNumber { get; set; }
        public string DiskName { get; set; }
        public string VolumeLabel { get; set; }
        public string PartitionDeviceID { get; set; }
        public override string ToString()
        {
            return string.Format("{0} ({1})", DiskName, VolumeLabel);
        }
    }
}
