using System;
using System.ComponentModel.DataAnnotations;

namespace WeatherDataAnalysis.Models
{
    public class Measurement
    {
        [Key]
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Location { get; set; } = string.Empty;
        public double? Temperature { get; set; }
        public int? Humidity { get; set; }
    }
}