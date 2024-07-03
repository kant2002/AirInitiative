using AirInitiative.Core;
using System.Globalization;
using System.Net.Http.Json;

var client = new HttpClient();
var root = await client.GetFromJsonAsync<Root>("https://api.openaq.org/v3/locations?order_by=id&sort_order=asc&countries_id=15&providers_id=119&providers_id=166&coordinates=43.238949%2C76.889709&radius=25000&limit=100&page=1");
var storageLocation = "data";
Directory.CreateDirectory(storageLocation);
foreach (var r in root.results)
{
    Console.WriteLine($"{r.id},{r.name}. From: {r.datetimeFirst.utc.Date:u} To: {r.datetimeLast.utc.Date:u}");
    for (DateTime d = r.datetimeFirst.utc.Date; d <= r.datetimeLast.utc.Date; d = d.AddDays(1))
    {
        var targetFile = $"{storageLocation}/location-{r.id}-{d:yyyyMMdd}.csv.gz";
        if (File.Exists(targetFile))
        {
            continue;
        }
        else
        {
            var url = $"https://openaq-data-archive.s3.amazonaws.com/records/csv.gz/locationid={r.id}/year={d:yyyy}/month={d:MM}/location-{r.id}-{d:yyyyMMdd}.csv.gz";
            var streamResponse = await client.GetAsync(url);
            if (!streamResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"Cannot download {url}");
                File.AppendAllLines("broken-urls.txt", [url]);
                continue;
            }

            var stream = streamResponse.Content.ReadAsStream();
            using var file = File.OpenWrite($"{storageLocation}/location-{r.id}-{d:yyyyMMdd}.csv.gz");
            await stream.CopyToAsync(file);
        }
    }
}
//var locations = root.results.Select(r => new LocationInformation("openaq.org", r.id.ToString(), false, r.name, r.coordinates.longitude, r.coordinates.latitude));
//var reports = await ReportLoader.Load(File.OpenRead(args[0]));

//WriteLocationsFile("locations.csv", locations.Union(GetLocations(reports)));
// await ExportFromOldFormat(args[0]);

async Task ExportFromOldFormat(string fileName)
{

    ReportLoader.ReportProduced += (report) =>
    {
        Console.WriteLine($"Station {report.Code} at {report.LocationName} with {(report.IsManualCollection ? " manual" : "automatic")} collection");
    };
    ReportLoader.ErrorRowDetected += (row) =>
    {
        Console.WriteLine($"Empty value in cell {row.CellReference}({row.MeasurementName}) at sheet {row.SheetName} at date {row.MeasureDateTime}");
    };
    var reports = await ReportLoader.Load(File.OpenRead(fileName));
    WriteLocationsFile("locations.csv", GetLocations(reports));
    WriteExportFile(reports);
}

void WriteExportFile(MeasurementReport[] reports)
{
    using CsvHelper.CsvWriter writer = new CsvHelper.CsvWriter(new StreamWriter("report.csv", false, System.Text.Encoding.UTF8), CultureInfo.InvariantCulture);
    writer.WriteRecords(reports.SelectMany(x => x.Measurements.Select(item => new
    {
        x.Code,
        x.IsManualCollection,
        item.MeasureDateLocal,
        item.SO2,
        item.NO2,
        item.CO,
        item.PM25,
        item.PM100,
    })));
}
void WriteLocationsFile(string fileName, IEnumerable<LocationInformation> locations)
{
    using CsvHelper.CsvWriter writer = new CsvHelper.CsvWriter(new StreamWriter(fileName, false, System.Text.Encoding.UTF8), CultureInfo.InvariantCulture);
    writer.WriteRecords(locations);
}

static IEnumerable<LocationInformation> GetLocations(MeasurementReport[] reports)
{
    return reports.Select(x => new LocationInformation(
        "XLSX",
        x.Code,
        x.IsManualCollection,
        x.LocationName,
        null,
        null
    )).Distinct();
}

// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
public class Coordinates
{
    public double latitude { get; set; }
    public double longitude { get; set; }
}

public class Country
{
    public int id { get; set; }
    public string code { get; set; }
    public string name { get; set; }
}

public class DatetimeFirst
{
    public DateTime utc { get; set; }
    public DateTime local { get; set; }
}

public class DatetimeLast
{
    public DateTime utc { get; set; }
    public DateTime local { get; set; }
}

public class Instrument
{
    public int id { get; set; }
    public string name { get; set; }
}

public class License
{
    public int id { get; set; }
    public string url { get; set; }
    public string dateFrom { get; set; }
    public object dateTo { get; set; }
    public string description { get; set; }
}

public class Meta
{
    public string name { get; set; }
    public string website { get; set; }
    public int page { get; set; }
    public int limit { get; set; }
    public int found { get; set; }
}

public class Owner
{
    public int id { get; set; }
    public string name { get; set; }
}

public class Parameter
{
    public int id { get; set; }
    public string name { get; set; }
    public string units { get; set; }
    public string displayName { get; set; }
}

public class Provider
{
    public int id { get; set; }
    public string name { get; set; }
}

public class Result
{
    public int id { get; set; }
    public string name { get; set; }
    public string locality { get; set; }
    public string timezone { get; set; }
    public Country country { get; set; }
    public Owner owner { get; set; }
    public Provider provider { get; set; }
    public bool isMobile { get; set; }
    public bool isMonitor { get; set; }
    public List<Instrument> instruments { get; set; }
    public List<Sensor> sensors { get; set; }
    public Coordinates coordinates { get; set; }
    public List<License> licenses { get; set; }
    public List<double> bounds { get; set; }
    public object distance { get; set; }
    public DatetimeFirst datetimeFirst { get; set; }
    public DatetimeLast datetimeLast { get; set; }
}

public class Root
{
    public Meta meta { get; set; }
    public List<Result> results { get; set; }
}

public class Sensor
{
    public int id { get; set; }
    public string name { get; set; }
    public Parameter parameter { get; set; }
}

internal record LocationInformation(string Provider, string Code, bool IsManualCollection, string LocationName, double? Longitude, double? Latitude)
{
    public override int GetHashCode()
    {
        return HashCode.Combine(Provider, Code, IsManualCollection, LocationName);
    }
}