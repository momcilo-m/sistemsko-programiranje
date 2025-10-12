using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OpenMeteo.Models
{
    public class AirQuality
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        //public double GenerationtimeMs { get; set; }
        //public int UtcOffsetSeconds { get; set; }
        //public string Timezone { get; set; }
        //public string TimezoneAbbreviation { get; set; }
        //public double Elevation { get; set; }

        
        //public HourlyUnits Hourly_Units { get; set; }
        [JsonPropertyName("hourly")]
        public HourlyData Hourly { get; set; }

        public bool? Error{ get; set; }
        public string? Reason { get; set; } 
    }
}
