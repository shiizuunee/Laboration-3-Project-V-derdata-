using WeatherDataAnalysis.Data;
using WeatherDataAnalysis.Services;
using Spectre.Console;

namespace WeatherDataAnalysis.Presentation
{
    public class AnalysisPresenter
    {
        private readonly AnalysisService _analysisService;
        private readonly WeatherDataContext _context;

        public AnalysisPresenter(WeatherDataContext context, AnalysisService analysisService)
        {
            _context = context;
            _analysisService = analysisService;
        }

        public void ShowOutdoorAnalyses()
        {
            var firstDate = GetFirstDateForLocation("Ute");
            if (!firstDate.HasValue)
            {
                AnsiConsole.MarkupLine("[red]No outdoor data found![/]");
                return;
            }

            ShowSectionHeader("OUTDOOR ANALYSIS", Color.Blue);

            ShowAverageTemperature(firstDate.Value, "Ute");
            ShowTopDays("WARMEST", _analysisService.SortByTemperature("Ute").Take(5), d => $"{d.AvgTemp:F1}°C", Color.Orange1);
            ShowTopDays("COLDEST", _analysisService.SortByTemperature("Ute").TakeLast(5).Reverse(), d => $"{d.AvgTemp:F1}°C", Color.Blue);
            ShowTopDays("DRIEST", _analysisService.SortByHumidity("Ute").Take(5), d => $"{d.AvgHumidity:F1}%", Color.Yellow);
            ShowTopDays("MOST HUMID", _analysisService.SortByHumidity("Ute").TakeLast(5).Reverse(), d => $"{d.AvgHumidity:F1}%", Color.Aqua);
            ShowTopDays("HIGHEST MOLD RISK", _analysisService.SortByMoldRisk("Ute").TakeLast(5).Reverse(), d => $"Risk: {d.MoldRisk:F1}", Color.Red);
            ShowTopDays("LOWEST MOLD RISK", _analysisService.SortByMoldRisk("Ute").Take(5), d => $"Risk: {d.MoldRisk:F1}", Color.Green);
            ShowMeteorologicalSeasons();
        }

        public void ShowIndoorAnalyses()
        {
            var firstDate = GetFirstDateForLocation("Inne");
            if (!firstDate.HasValue) return;

            ShowSectionHeader("INDOOR ANALYSIS", Color.Green);

            ShowAverageTemperature(firstDate.Value, "Inne");
            ShowTopDays("WARMEST INDOOR", _analysisService.SortByTemperature("Inne").Take(5), d => $"{d.AvgTemp:F1}°C", Color.Orange1);
            ShowTopDays("COLDEST INDOOR", _analysisService.SortByTemperature("Inne").TakeLast(5).Reverse(), d => $"{d.AvgTemp:F1}°C", Color.Blue);
            ShowTopDays("MOST HUMID INDOOR", _analysisService.SortByHumidity("Inne").TakeLast(5).Reverse(), d => $"{d.AvgHumidity:F1}%", Color.Aqua);
            ShowTopDays("HIGHEST MOLD RISK INDOOR", _analysisService.SortByMoldRisk("Inne").TakeLast(5).Reverse(), d => $"Risk: {d.MoldRisk:F1}", Color.Red);
        }
        public void ShowBalconyDoorAnalysis()
        {
            ShowSectionHeader("BALCONY DOOR ANALYSIS (BONUS)", Color.Purple);

            var pairs = GetIndoorOutdoorPairs();
            if (!pairs.Any())
            {
                AnsiConsole.MarkupLine("[grey]No paired measurements found[/]");
                return;
            }

            var doorOpenDays = pairs
                .GroupBy(p => p.Date.Date)
                .Select(g =>
                {
                    var diffs = g.Select(x => x.IndoorTemp - x.OutdoorTemp).ToList();
                    var avgDiff = diffs.Average();
                    var openCount = g.Count(x => (x.IndoorTemp - x.OutdoorTemp) < avgDiff - 1.0);
                    return new { Date = g.Key, OpenEvents = openCount };
                })
                .Where(d => d.OpenEvents > 0)
                .OrderByDescending(d => d.OpenEvents)
                .Take(5)
                .ToList();

            if (!doorOpenDays.Any())
            {
                AnsiConsole.MarkupLine("[grey]No estimated door opening events found[/]");
                return;
            }

            AnsiConsole.MarkupLine("[bold purple]TOP 5 DAYS WITH MOST ESTIMATED DOOR OPENINGS[/]");
            int rank = 1;
            foreach (var day in doorOpenDays)
            {
                AnsiConsole.MarkupLine($"  [grey]{rank}.[/] {day.Date:yyyy-MM-dd} - [purple]{day.OpenEvents} estimated openings[/]");
                rank++;
            }
            AnsiConsole.WriteLine();
        }

        public void ShowTemperatureDifferenceAnalysis()
        {
            ShowSectionHeader("TEMPERATURE DIFFERENCE ANALYSIS (BONUS)", Color.Teal);

            var daily = _context.Measurements
                .Where(m => m.Temperature.HasValue && (m.Location == "Inne" || m.Location == "Ute"))
                .GroupBy(m => new { Date = m.Date.Date, m.Location })
                .Select(g => new
                {
                    Day = g.Key.Date,
                    Location = g.Key.Location,
                    AvgTemp = g.Average(m => m.Temperature!.Value)
                })
                .ToList();

            var joined = (from indoor in daily.Where(d => d.Location == "Inne")
                          join outdoor in daily.Where(d => d.Location == "Ute")
                          on indoor.Day equals outdoor.Day
                          select new
                          {
                              Date = indoor.Day,
                              IndoorTemp = indoor.AvgTemp,
                              OutdoorTemp = outdoor.AvgTemp,
                              Diff = Math.Abs(indoor.AvgTemp - outdoor.AvgTemp)
                          })
                          .ToList();

            if (!joined.Any())
            {
                AnsiConsole.MarkupLine("[grey]No data found[/]");
                return;
            }

            AnsiConsole.MarkupLine("[bold teal]TOP 5 DAYS WITH SMALLEST TEMPERATURE DIFFERENCE[/]");
            var smallest = joined.OrderBy(d => d.Diff).Take(5);
            int rank = 1;
            foreach (var day in smallest)
            {
                AnsiConsole.MarkupLine($"  [grey]{rank}.[/] {day.Date:yyyy-MM-dd} - [teal]Diff: {day.Diff:F1}°C[/] (Indoor: {day.IndoorTemp:F1}°C, Outdoor: {day.OutdoorTemp:F1}°C)");
                rank++;
            }
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine("[bold orange1]TOP 5 DAYS WITH LARGEST TEMPERATURE DIFFERENCE[/]");
            var largest = joined.OrderByDescending(d => d.Diff).Take(5);
            rank = 1;
            foreach (var day in largest)
            {
                AnsiConsole.MarkupLine($"  [grey]{rank}.[/] {day.Date:yyyy-MM-dd} - [orange1]Diff: {day.Diff:F1}°C[/] (Indoor: {day.IndoorTemp:F1}°C, Outdoor: {day.OutdoorTemp:F1}°C)");
                rank++;
            }
            AnsiConsole.WriteLine();
        }

        private DateTime? GetFirstDateForLocation(string location)
        {
            var dates = _context.Measurements
                .Where(m => m.Location == location)
                .Select(m => m.Date.Date)
                .ToList();

            return dates.Any() ? dates.Min() : (DateTime?)null;
        }

        private void ShowSectionHeader(string title, Color color)
        {
            var rule = new Rule($"[bold {color}]{title}[/]");
            rule.Style = Style.Parse(color.ToString().ToLower());
            AnsiConsole.Write(rule);
            AnsiConsole.WriteLine();
        }

        private void ShowAverageTemperature(DateTime date, string location)
        {
            var avgTemp = _analysisService.GetAverageTemperature(date, location);
            if (avgTemp.HasValue)
            {
                AnsiConsole.MarkupLine($"[cyan]Average temperature on {date:yyyy-MM-dd}:[/] [white]{avgTemp:F1}°C[/]\n");
            }
        }

        private void ShowTopDays<T>(string title, System.Collections.Generic.IEnumerable<T> data, Func<T, string> valueFormatter, Color color)
        {
            AnsiConsole.MarkupLine($"[bold {color}]TOP 5 {title}[/]");
            int rank = 1;
            foreach (var item in data)
            {
                var dateProperty = item.GetType().GetProperty("Date");
                if (dateProperty != null)
                {
                    var date = (DateTime)dateProperty.GetValue(item);
                    var value = valueFormatter(item);
                    AnsiConsole.MarkupLine($"  [grey]{rank}.[/] {date:yyyy-MM-dd} - [{color}]{value}[/]");
                    rank++;
                }
            }
            AnsiConsole.WriteLine();
        }

        private void ShowMeteorologicalSeasons()
        {
            AnsiConsole.MarkupLine("[bold yellow]METEOROLOGICAL SEASONS[/]");
            var autumnDate = _analysisService.FindMeteorologicalAutumn();
            var winterDate = _analysisService.FindMeteorologicalWinter();

            if (autumnDate.HasValue)
                AnsiConsole.MarkupLine($"  [green]Autumn started:[/] {autumnDate.Value:yyyy-MM-dd}");
            else
                AnsiConsole.MarkupLine("  [grey]Autumn: Not found[/]");

            if (winterDate.HasValue)
                AnsiConsole.MarkupLine($"  [blue]Winter started:[/] {winterDate.Value:yyyy-MM-dd}");
            else
                AnsiConsole.MarkupLine("  [grey]Winter: Not found (mild winter 2016)[/]");

            AnsiConsole.WriteLine();
        }

        private class IndoorOutdoorPair
        {
            public DateTime Date { get; set; }
            public double IndoorTemp { get; set; }
            public double OutdoorTemp { get; set; }
        }

        private System.Collections.Generic.List<IndoorOutdoorPair> GetIndoorOutdoorPairs()
        {
            return (from indoor in _context.Measurements
                    where indoor.Location == "Inne" && indoor.Temperature.HasValue
                    join outdoor in _context.Measurements on indoor.Date equals outdoor.Date
                    where outdoor.Location == "Ute" && outdoor.Temperature.HasValue
                    select new IndoorOutdoorPair
                    {
                        Date = indoor.Date,
                        IndoorTemp = indoor.Temperature!.Value,
                        OutdoorTemp = outdoor.Temperature!.Value
                    })
                    .ToList();
        }
    }
}