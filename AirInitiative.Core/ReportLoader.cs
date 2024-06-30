using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace AirInitiative.Core;

public static class ReportLoader
{
    public static async Task<MeasurementReport[]> Load(Stream stream)
    {
        using var document = SpreadsheetDocument.Open(stream, false);
        var workbookPart = document.WorkbookPart;
        var workbook = workbookPart.Workbook;

        var sheets = workbook.Descendants<Sheet>();
        List<MeasurementReport> reports = new();
        foreach (var sheet in sheets)
        {
            var report = LoadMeasurementReport(workbookPart, sheet);
            Console.WriteLine($"Station {report.Code} at {report.LocationName} with {(report.IsManualCollection ? " manual" : "automatic")} collection");
            reports.Add(report);
        }

        return reports.ToArray();
    }

    private static MeasurementReport LoadMeasurementReport(WorkbookPart? workbookPart, Sheet sheet)
    {
        var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id);
        var sharedStringTable = workbookPart.SharedStringTablePart.SharedStringTable;
        string? stationCode = null;
        string? locationName = null;
        var rows = worksheetPart.Worksheet.Descendants<Row>();
        foreach (var row in rows)
        {
            int count = row.Elements<Cell>().Count();
            if (stationCode is not null && locationName is not null) break;

            foreach (Cell c in row.Elements<Cell>())
            {
                if (c.CellReference == "B2")
                {
                    var cellValue = c.InnerText;
                    if (c.DataType is not null && c.DataType == CellValues.SharedString)
                    {
                        stationCode = sharedStringTable.ElementAt(int.Parse(cellValue)).InnerText;
                    }
                    else
                    {
                        stationCode = c.CellValue!.InnerText;
                    }
                    if (stationCode is not null && locationName is not null) break;
                }
                if (c.CellReference == "C2")
                {
                    var cellValue = c.InnerText;
                    if (c.DataType is not null && c.DataType == CellValues.SharedString)
                    {
                        locationName = sharedStringTable.ElementAt(int.Parse(cellValue)).InnerText;
                    }
                    else
                    {
                        locationName = c.CellValue!.InnerText;
                    }
                    if (stationCode is not null && locationName is not null) break;

                }
            }
        }

        bool isManualCollection = true;
        stationCode = stationCode ?? throw new InvalidOperationException("Station code is missing");
        if (sheet.Name?.HasValue == true)
        {
            isManualCollection = sheet.Name.Value!.Contains("-руч");
        }

        return new MeasurementReport()
        {
            Code = stationCode,
            LocationName = locationName ?? throw new InvalidOperationException("Location name is missing"),
            IsManualCollection = isManualCollection,
        };
    }
}
