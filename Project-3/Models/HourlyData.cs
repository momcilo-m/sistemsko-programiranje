using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OpenMeteo.Models
{
    public class HourlyData
    {
        [JsonPropertyName("time")]
        public List<string> Time { get; set; }

        [JsonPropertyName("pm10")]
        public List<double?> Pm10 { get; set; }

        [JsonPropertyName("pm2_5")]
        public List<double?> Pm2_5 { get; set; }

        [JsonPropertyName("carbon_monoxide")]
        public List<double?> Carbon_Monoxide { get; set; }

        [JsonPropertyName("nitrogen_dioxide")]
        public List<double?> Nitrogen_Dioxide { get; set; }
    }
}
