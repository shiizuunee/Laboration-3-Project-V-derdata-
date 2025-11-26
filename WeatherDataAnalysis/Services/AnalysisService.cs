using WeatherDataAnalysis.Data;

namespace WeatherDataAnalysis.Services
{
    public class DayTemperature
    {
        public DateTime Date { get; set; }
        public double AvgTemp { get; set; }
    }

    public class DayHumidity
    {
        public DateTime Date { get; set; }
        public double AvgHumidity { get; set; }
    }

    public class DayMoldRisk
    {
        public DateTime Date { get; set; }
        public double MoldRisk { get; set; }
    }
    public class AnalysisService
    {
        private readonly WeatherDataContext _context;

        public AnalysisService(WeatherDataContext context)
        {
            _context = context;
        }

        public double? GetAverageTemperature(DateTime date, string location)
        {
            var measurements = _context.Measurements
                .Where(m => m.Date.Date == date.Date && m.Location == location && m.Temperature.HasValue)
                .ToList();

            return measurements.Any() ? measurements.Average(m => m.Temperature!.Value) : null;
        }

        public List<DayTemperature> SortByTemperature(string location)
        {
            return _context.Measurements
                .Where(m => m.Location == location && m.Temperature.HasValue)
                .GroupBy(m => m.Date.Date)
                .Select(g => new DayTemperature
                {
                    Date = g.Key,
                    AvgTemp = g.Average(m => m.Temperature!.Value)
                })
                .OrderByDescending(x => x.AvgTemp)
                .ToList();
        }

        public List<DayHumidity> SortByHumidity(string location)
        {
            return _context.Measurements
                .Where(m => m.Location == location && m.Humidity.HasValue)
                .GroupBy(m => m.Date.Date)
                .Select(g => new DayHumidity
                {
                    Date = g.Key,
                    AvgHumidity = g.Average(m => m.Humidity!.Value)
                })
                .OrderBy(x => x.AvgHumidity)
                .ToList();
        }

        private double CalculateMoldRisk(double temp, double humidity)
        {
            double humidityRisk = humidity > 70 ? (humidity - 70) * 2 : 0;

            double tempRisk = (temp >= 15 && temp <= 30)
                ? (30 - Math.Abs(22.5 - temp))
                : 0;

            return humidityRisk + tempRisk;
        }

        public List<DayMoldRisk> SortByMoldRisk(string location)
        {
            var dailyData = _context.Measurements
                .Where(m => m.Location == location && m.Temperature.HasValue && m.Humidity.HasValue)
                .GroupBy(m => m.Date.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    AvgTemp = g.Average(m => m.Temperature!.Value),
                    AvgHumidity = g.Average(m => m.Humidity!.Value)
                })
                .ToList(); 

            return dailyData
                .Select(x => new DayMoldRisk
                {
                    Date = x.Date,
                    MoldRisk = CalculateMoldRisk(x.AvgTemp, x.AvgHumidity)
                })
                .OrderBy(x => x.MoldRisk)
                .ToList();
        }

        public DateTime? FindMeteorologicalAutumn()
        {
            var dailyAvgTemp = _context.Measurements
                .Where(m => m.Location == "Ute" && m.Temperature.HasValue)
                .GroupBy(m => m.Date.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    AvgTemp = g.Average(m => m.Temperature!.Value)
                })
                .OrderBy(x => x.Date)
                .ToList();

            for (int i = 0; i <= dailyAvgTemp.Count - 5; i++)
            {
                bool autumnFound = true;
                for (int j = 0; j < 5; j++)
                {
                    if (dailyAvgTemp[i + j].AvgTemp >= 10)
                    {
                        autumnFound = false;
                        break;
                    }
                }

                if (autumnFound)
                {
                    return dailyAvgTemp[i].Date;
                }
            }

            return null;
        }

        public DateTime? FindMeteorologicalWinter()
        {
            var dailyAvgTemp = _context.Measurements
                .Where(m => m.Location == "Ute" && m.Temperature.HasValue)
                .GroupBy(m => m.Date.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    AvgTemp = g.Average(m => m.Temperature!.Value)
                })
                .OrderBy(x => x.Date)
                .ToList();

            for (int i = 0; i <= dailyAvgTemp.Count - 5; i++)
            {
                bool winterFound = true;
                for (int j = 0; j < 5; j++)
                {
                    if (dailyAvgTemp[i + j].AvgTemp >= 0)
                    {
                        winterFound = false;
                        break;
                    }
                }

                if (winterFound)
                {
                    return dailyAvgTemp[i].Date;
                }
            }

            return null;
        }
    }
}