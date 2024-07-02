using AirInitiative.Core;
using System.Globalization;

await ExportFromOldFormat(args[0]);

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
    WriteLocationsFile(reports);
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
void WriteLocationsFile(MeasurementReport[] reports)
{
    using CsvHelper.CsvWriter writer = new CsvHelper.CsvWriter(new StreamWriter("locations.csv", false, System.Text.Encoding.UTF8), CultureInfo.InvariantCulture);
    writer.WriteRecords(reports.Select(x => new
    {
        x.Code,
        x.IsManualCollection,
        x.LocationName,
    }).Distinct());
}