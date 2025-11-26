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
            var title = new Rule("WEATHER ANALYSIS")
                .RuleStyle("blue")
                .Centered();

            AnsiConsole.Write(title);
            AnsiConsole.MarkupLine("[grey]Temperature and Humidity Data Analysis System[/]\n");
        }

        static void InitializeDatabase(WeatherDataContext context)
        {
            AnsiConsole.Status().Start("Checking database...", ctx =>
            {
                context.Database.EnsureCreated();
                ctx.Status("Database ready");
            });

            AnsiConsole.MarkupLine("[green]Database ready: WeatherData.db[/]");

            var measurementCount = context.Measurements.Count();

            if (measurementCount == 0)
            {
                AnsiConsole.MarkupLine("[yellow]Database is empty. Importing CSV data...[/]");
                ImportCsvData(context);
                measurementCount = context.Measurements.Count();
            }

            AnsiConsole.MarkupLine($"[green]Database contains {measurementCount:N0} measurements[/]\n");
        }

        static void ImportCsvData(WeatherDataContext context)
        {
            var csvPath = "TempFuktData.csv";

            if (!File.Exists(csvPath))
            {
                AnsiConsole.MarkupLine($"[red]Could not find {csvPath}[/]");
                return;
            }

            AnsiConsole.Status().Start("Reading CSV file...", ctx =>
            {
                var csvService = new CsvImportService();
                var measurements = csvService.ImportCsvFile(csvPath);

                if (measurements.Count == 0)
                {
                    AnsiConsole.MarkupLine("[red]No measurements imported[/]");
                    return;
                }

                ctx.Status("Saving to database...");
                context.Measurements.AddRange(measurements);
                context.SaveChanges();
            });
        }

        static void ShowMenu(AnalysisPresenter presenter)
        {
            while (true)
            {
                Console.Clear();
                DisplayHeader();

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[cyan]What would you like to analyze?[/]")
                        .PageSize(10)
                        .AddChoices(new[] {
                            "1. All Outdoor Analysis",
                            "2. All Indoor Analysis",
                            "3. Balcony Door Analysis",
                            "4. Temperature Difference Analysis",
                            "5. Run ALL Analyses",
                            "6. Exit"
                        }));

                Console.Clear();

                switch (choice[0])
                {
                    case '1':
                        presenter.ShowOutdoorAnalyses();
                        break;
                    case '2':
                        presenter.ShowIndoorAnalyses();
                        break;
                    case '3':
                        presenter.ShowBalconyDoorAnalysis();
                        break;
                    case '4':
                        presenter.ShowTemperatureDifferenceAnalysis();
                        break;
                    case '5':
                        presenter.ShowOutdoorAnalyses();
                        presenter.ShowIndoorAnalyses();
                        presenter.ShowBalconyDoorAnalysis();
                        presenter.ShowTemperatureDifferenceAnalysis();
                        break;
                    case '6':
                        AnsiConsole.MarkupLine("[yellow]Exiting...[/]");
                        return;
                }

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[grey]Press any key to return to menu...[/]");
                Console.ReadKey();
            }
        }
    }
}