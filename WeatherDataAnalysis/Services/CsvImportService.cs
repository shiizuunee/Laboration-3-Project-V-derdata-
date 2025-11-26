using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using WeatherDataAnalysis.Models;

namespace WeatherDataAnalysis.Services
{
    public class CsvImportService
    {
        public List<Measurement> ImportCsvFile(string filePath)
        {
            var measurements = new List<Measurement>();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,     
                Delimiter = ",",              
                BadDataFound = null          
            };

            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, config))
            {
                csv.Read();
                csv.ReadHeader();

                while (csv.Read())
                {
                    try
                    {
                        var dateStr = csv.GetField<string>("Datum");
                        var location = csv.GetField<string>("Plats");
                        var tempStr = csv.GetField<string>("Temp");
                        var humidityStr = csv.GetField<string>("Luftfuktighet");

                        DateTime date;
                        double? temperature = null;
                        int? humidity = null;

                        if (DateTime.TryParse(dateStr, out var parsedDate))
                        {
                            date = parsedDate;
                        }
                        else
                        {
                            continue;
                        }

                        if (!string.IsNullOrWhiteSpace(tempStr))
                        {
                            if (double.TryParse(tempStr.Replace(",", "."),
                                NumberStyles.Any,
                                CultureInfo.InvariantCulture,
                                out var parsedTemp))
                            {
                                temperature = parsedTemp;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(humidityStr))
                        {
                            if (int.TryParse(humidityStr, out var parsedHumidity))
                            {
                                humidity = parsedHumidity;
                            }
                        }

                        measurements.Add(new Measurement
                        {
                            Date = date,
                            Location = location ?? "",
                            Temperature = temperature,
                            Humidity = humidity
                        });
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            return measurements;
        }
    }
}