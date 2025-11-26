using System;
using System.Linq;
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
            ShowLocationAnalyses("Ute", "UTOMHUSANALYS", Color.Blue, showSeasons: true, showDriest: true, showLowestMold: true);
        }

        public void ShowIndoorAnalyses()
        {
            ShowLocationAnalyses("Inne", "INOMHUSANALYS", Color.Green, showSeasons: false, showDriest: false, showLowestMold: false);
        }

        private void ShowLocationAnalyses(string location, string header, Color headerColor,
                                         bool showSeasons, bool showDriest, bool showLowestMold)
        {
            var firstDate = GetFirstDateForLocation(location);
            if (!firstDate.HasValue)
            {
                AnsiConsole.MarkupLine($"[red]Ingen data hittades för {location}![/]");
                return;
            }

            ShowSectionHeader(header, headerColor);
            ShowAverageTemperature(firstDate.Value, location);

            var suffix = location == "Inne" ? " INOMHUS" : "";

            ShowTopDays($"VARMASTE DAGARNA{suffix}", _analysisService.SortByTemperature(location).Take(5),
                       d => $"{d.AvgTemp:F1}°C", Color.Orange1);
            ShowTopDays($"KALLASTE DAGARNA{suffix}", _analysisService.SortByTemperature(location).TakeLast(5).Reverse(),
                       d => $"{d.AvgTemp:F1}°C", Color.Blue);

            if (showDriest)
                ShowTopDays("TORRASTE DAGARNA", _analysisService.SortByHumidity(location).Take(5),
                           d => $"{d.AvgHumidity:F1}%", Color.Yellow);

            ShowTopDays($"FUKTIGASTE DAGARNA{suffix}", _analysisService.SortByHumidity(location).TakeLast(5).Reverse(),
                       d => $"{d.AvgHumidity:F1}%", Color.Aqua);
            ShowTopDays($"HÖGST MÖGELRISK{suffix}", _analysisService.SortByMoldRisk(location).TakeLast(5).Reverse(),
                       d => $"Risk: {d.MoldRisk:F1}", Color.Red);

            if (showLowestMold)
                ShowTopDays("LÄGST MÖGELRISK", _analysisService.SortByMoldRisk(location).Take(5),
                           d => $"Risk: {d.MoldRisk:F1}", Color.Green);

            if (showSeasons)
                ShowMeteorologicalSeasons();
        }

        public void ShowBalconyDoorAnalysis()
        {
            ShowSectionHeader("BALKONGDÖRR-ANALYS", Color.Purple);

            var pairs = GetIndoorOutdoorPairs();
            if (!pairs.Any())
            {
                AnsiConsole.MarkupLine("[grey]Inga parade mätningar hittades[/]");
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
                AnsiConsole.MarkupLine("[grey]Inga uppskattade dörröppningar hittades[/]");
                return;
            }

            AnsiConsole.MarkupLine("[bold purple]TOPP 5 DAGAR MED MEST UPPSKATTADE DÖRRÖPPNINGAR[/]");
            int rank = 1;
            foreach (var day in doorOpenDays)
            {
                AnsiConsole.MarkupLine($"  [grey]{rank}.[/] {day.Date:yyyy-MM-dd} - [purple]{day.OpenEvents} uppskattade öppningar[/]");
                rank++;
            }
            AnsiConsole.WriteLine();
        }

        public void ShowTemperatureDifferenceAnalysis()
        {
            ShowSectionHeader("TEMPERATURSKILLNADS-ANALYS", Color.Teal);

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
                AnsiConsole.MarkupLine("[grey]Ingen data hittades[/]");
                return;
            }

            AnsiConsole.MarkupLine("[bold teal]TOPP 5 DAGAR MED MINST TEMPERATURSKILLNAD[/]");
            var smallest = joined.OrderBy(d => d.Diff).Take(5);
            int rank = 1;
            foreach (var day in smallest)
            {
                AnsiConsole.MarkupLine($"  [grey]{rank}.[/] {day.Date:yyyy-MM-dd} - [teal]Skillnad: {day.Diff:F1}°C[/] (Inne: {day.IndoorTemp:F1}°C, Ute: {day.OutdoorTemp:F1}°C)");
                rank++;
            }
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine("[bold orange1]TOPP 5 DAGAR MED STÖRST TEMPERATURSKILLNAD[/]");
            var largest = joined.OrderByDescending(d => d.Diff).Take(5);
            rank = 1;
            foreach (var day in largest)
            {
                AnsiConsole.MarkupLine($"  [grey]{rank}.[/] {day.Date:yyyy-MM-dd} - [orange1]Skillnad: {day.Diff:F1}°C[/] (Inne: {day.IndoorTemp:F1}°C, Ute: {day.OutdoorTemp:F1}°C)");
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
                AnsiConsole.MarkupLine($"[cyan]Medeltemperatur den {date:yyyy-MM-dd}:[/] [white]{avgTemp:F1}°C[/]\n");
            }
        }

        private void ShowTopDays<T>(string title, System.Collections.Generic.IEnumerable<T> data, Func<T, string> valueFormatter, Color color)
        {
            AnsiConsole.MarkupLine($"[bold {color}]TOPP 5 {title}[/]");
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
            AnsiConsole.MarkupLine("[bold yellow]METEOROLOGISKA ÅRSTIDER[/]");
            var autumnDate = _analysisService.FindMeteorologicalAutumn();
            var winterDate = _analysisService.FindMeteorologicalWinter();

            if (autumnDate.HasValue)
                AnsiConsole.MarkupLine($"  [green]Hösten började:[/] {autumnDate.Value:yyyy-MM-dd}");
            else
                AnsiConsole.MarkupLine("  [grey]Höst: Hittades inte i datasetet[/]");

            if (winterDate.HasValue)
                AnsiConsole.MarkupLine($"  [blue]Vintern började:[/] {winterDate.Value:yyyy-MM-dd}");
            else
                AnsiConsole.MarkupLine("  [grey]Vinter: Hittades inte (mild vinter 2016)[/]");

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