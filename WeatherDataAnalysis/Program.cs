using System;
using System.IO;
using System.Linq;
using WeatherDataAnalysis.Data;
using WeatherDataAnalysis.Services;
using WeatherDataAnalysis.Presentation;
using Spectre.Console;

namespace WeatherDataAnalysis
{
    class Program
    {
        static void Main(string[] args)
        {
            DisplayHeader();

            using (var context = new WeatherDataContext())
            {
                InitializeDatabase(context);

                var analysisService = new AnalysisService(context);
                var presenter = new AnalysisPresenter(context, analysisService);

                ShowMenu(presenter);
            }
        }

        static void DisplayHeader()
        {
            Console.Clear();
            var title = new Rule("[bold blue]Vaderanalys[/]")
                .RuleStyle("blue")
                .Centered();
            AnsiConsole.Write(title);
            AnsiConsole.MarkupLine("[grey]Temperatur och Luftfuktighets Analyssystem[/]\n");
        }

        static void InitializeDatabase(WeatherDataContext context)
        {
            AnsiConsole.MarkupLine("[blue]Kontrollerar databas...[/]");
            context.Database.EnsureCreated();
            AnsiConsole.MarkupLine("  [green]Databas redo: WeatherData.db[/]\n");

            var measurementCount = context.Measurements.Count();

            if (measurementCount == 0)
            {
                AnsiConsole.MarkupLine("  [yellow]Databasen är tom. Importerar CSV-data...[/]");
                ImportCsvData(context);
                measurementCount = context.Measurements.Count();
                AnsiConsole.MarkupLine($"  [green]Importerade {measurementCount:N0} mätningar[/]\n");
            }
            else
            {
                AnsiConsole.MarkupLine($"  [green]Databasen innehåller {measurementCount:N0} mätningar[/]\n");
            }

            AnsiConsole.MarkupLine("[grey]Tryck på valfri tangent för att fortsätta...[/]");
            Console.ReadKey();
        }

        static void ImportCsvData(WeatherDataContext context)
        {
            var csvPath = "TempFuktData.csv";

            if (!File.Exists(csvPath))
            {
                AnsiConsole.MarkupLine($"  [red]Kunde inte hitta {csvPath}[/]");
                return;
            }

            AnsiConsole.MarkupLine("  [blue]Läser CSV-fil...[/]");
            var csvService = new CsvImportService();
            var measurements = csvService.ImportCsvFile(csvPath);

            if (measurements.Count == 0)
            {
                AnsiConsole.MarkupLine("  [red]Inga mätningar importerades[/]");
                return;
            }

            AnsiConsole.MarkupLine("  [blue]Sparar till databasen...[/]");
            context.Measurements.AddRange(measurements);
            context.SaveChanges();
        }

        static void ShowMenu(AnalysisPresenter presenter)
        {
            var menuOptions = new[] {
                "1. Utomhusanalys (Alla 6 analyser)",
                "2. Inomhusanalys (Alla 4 analyser)",
                "3. Balkongdörr-analys",
                "4. Temperaturskillnads-analys",
                "5. Kör ALLA analyser",
                "6. Avsluta"
            };

            while (true)
            {
                Console.Clear();
                AnsiConsole.Write(new Rule("[bold blue]VÄDERDATA ANALYSSYSTEM - MENY[/]")
                    .RuleStyle("blue").Centered());

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[cyan]Vad vill du analysera?[/]")
                        .PageSize(10)
                        .AddChoices(menuOptions));

                Console.Clear();

                switch (choice[0])
                {
                    case '1': presenter.ShowOutdoorAnalyses(); break;
                    case '2': presenter.ShowIndoorAnalyses(); break;
                    case '3': presenter.ShowBalconyDoorAnalysis(); break;
                    case '4': presenter.ShowTemperatureDifferenceAnalysis(); break;
                    case '5':
                        presenter.ShowOutdoorAnalyses();
                        presenter.ShowIndoorAnalyses();
                        presenter.ShowBalconyDoorAnalysis();
                        presenter.ShowTemperatureDifferenceAnalysis();
                        break;
                    case '6':
                        AnsiConsole.MarkupLine("[yellow]Avslutar programmet...[/]");
                        return;
                }

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[grey]Tryck på valfri tangent för att återgå till menyn...[/]");
                Console.ReadKey();
            }
        }
    }
}