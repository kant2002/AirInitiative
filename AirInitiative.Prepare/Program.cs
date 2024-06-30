using AirInitiative.Core;

ReportLoader.ReportProduced += (report) =>
{
    Console.WriteLine($"Station {report.Code} at {report.LocationName} with {(report.IsManualCollection ? " manual" : "automatic")} collection");
};
ReportLoader.ErrorRowDetected += (row) =>
{
    Console.WriteLine($"Empty value in cell {row.CellReference}({row.MeasurementName}) at sheet {row.SheetName} at date {row.MeasureDateTime}");
};
var reports = await ReportLoader.Load(File.OpenRead(args[0]));