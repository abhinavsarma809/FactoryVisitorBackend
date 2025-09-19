namespace FactoryVisitorApp.Models
{
    public class Zone
    {
        public int ZoneID { get; set; }
        public string? ZoneName { get; set; }
        public string? Description { get; set; }

      
        public  Zone() { }

        public Zone(int zoneID, string zoneName, string? description)
        {
            ZoneID = zoneID;
            ZoneName = zoneName;
            Description = description;
        }
    }
}
