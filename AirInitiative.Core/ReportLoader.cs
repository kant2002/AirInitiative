using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Globalization;

namespace AirInitiative.Core;

public static class ReportLoader
{
    public static Action<MeasurementReport>? ReportProduced;
    public static Action<ErrorRow>? ErrorRowDetected;
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
            ReportProduced?.Invoke(report);
            reports.Add(report);
        }

        return reports.ToArray();
    }

    private static MeasurementReport LoadMeasurementReport(WorkbookPart? workbookPart, Sheet sheet)
    {
        var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id);
        var sharedStringTable = workbookPart.SharedStringTablePart.SharedStringTable;
        bool isManualCollection = true;
        if (sheet.Name?.HasValue == true)
        {
            isManualCollection = sheet.Name.Value!.Contains("-руч");
        }

        string? stationCode = null;
        string? locationName = null;
        var rows = worksheetPart.Worksheet.Descendants<Row>();
        List<MeasurementItem> items = new();
        foreach (var row in rows)
        {
            int count = row.Elements<Cell>().Count();
            //if (stationCode is not null && locationName is not null) break;
            DateTime? MeasureDateLocal = null;
            double? SO2 = null;
            double? NO2 = null;
            double? CO = null;
            double? PM25 = null;
            double? PM100 = null;
            foreach (Cell c in row.Elements<Cell>())
            {
                if (stationCode is not null && locationName is not null)
                {
                    if (isManualCollection)
                    {
                        if (c.CellReference == "A3") goto next_row;
                        if (c.CellReference == "A4") goto next_row;
                        var column = c.CellReference.Value![0];
                        if (column == 'A')
                        {
                            var date = GetValue();
                            MeasureDateLocal = DateTime.FromOADate(int.Parse(date));
                        }
                        if (column == 'B')
                        {
                            var hour = GetValue();
                            if (MeasureDateLocal.HasValue)
                            {
                                MeasureDateLocal = MeasureDateLocal.Value.AddHours(int.Parse(hour));
                            }
                        }
                        if (column == 'C')
                        {
                            var v = GetValue();
                            if (v != null)
                            {
                                SO2 = double.Parse(v);
                            }
                        }
                        if (column == 'D')
                        {
                            var v = GetValue();
                            if (v != null)
                            {
                                NO2 = double.Parse(v);
                            }
                        }
                        if (column == 'E')
                        {
                            var v = GetValue();
                            if (v != null)
                            {
                                CO = double.Parse(v);
                            }
                        }
                    }
                    else
                    {
                        if (c.CellReference == "A3") goto next_row;
                        if (c.CellReference == "A4") goto next_row;
                        var column = c.CellReference.Value![0];
                        if (column == 'A')
                        {
                            var date = GetValue();
                            MeasureDateLocal = DateTime.ParseExact(date, "dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("ru-RU"));
                            //MeasureDateLocal = DateTime.FromOADate(double.Parse(date));
                        }
                        if (column == 'B')
                        {
                            var v = GetValue();
                            if (v != null)
                            {
                                if (v == "")
                                {
                                    ErrorRowDetected?.Invoke(new(sheet.Name!, c.CellReference.Value!, "PM25", MeasureDateLocal));
                                    continue;
                                }
                                PM25 = double.Parse(v);
                            }
                        }
                        if (column == 'C')
                        {
                            var v = GetValue();
                            if (v != null)
                            {
                                if (v == "")
                                {
                                    ErrorRowDetected?.Invoke(new(sheet.Name!, c.CellReference.Value!, "PM100", MeasureDateLocal));
                                    continue;
                                }
                                PM100 = double.Parse(v);
                            }
                        }
                        if (column == 'D')
                        {
                            var v = GetValue();
                            if (v != null)
                            {
                                if (v == "")
                                {
                                    ErrorRowDetected?.Invoke(new(sheet.Name!, c.CellReference.Value!, "SO2", MeasureDateLocal));
                                    continue;
                                }
                                SO2 = double.Parse(v);
                            }
                        }
                        if (column == 'E')
                        {
                            var v = GetValue();
                            if (v != null)
                            {
                                if (v == "")
                                {
                                    ErrorRowDetected?.Invoke(new(sheet.Name!, c.CellReference.Value!, "NO2", MeasureDateLocal));
                                    continue;
                                }
                                NO2 = double.Parse(v);
                            }
                        }
                        if (column == 'F')
                        {
                            var v = GetValue();
                            if (v != null)
                            {
                                if (v == "")
                                {
                                    ErrorRowDetected?.Invoke(new(sheet.Name!, c.CellReference.Value!, "CO", MeasureDateLocal));
                                    continue;
                                }
                                CO = double.Parse(v);
                            }
                        }
                    }
                }
                else
                {
                    if (c.CellReference == "B2")
                    {
                        stationCode = GetValue();
                    }
                    if (c.CellReference == "C2")
                    {
                        locationName = GetValue();
                    }
                    if (stationCode is not null && locationName is not null) break;
                }

                string? GetValue()
                {
                    var cellValue = c.InnerText;
                    if (c.DataType is not null && c.DataType == CellValues.SharedString)
                    {
                        return sharedStringTable.ElementAt(int.Parse(cellValue)).InnerText;
                    }
                    else
                    {
                        return c.CellValue?.InnerText;
                    }
                }
            }

            if (!MeasureDateLocal.HasValue) continue;

            MeasurementItem item = new(MeasureDateLocal.Value, SO2, NO2, CO, PM25, PM100);
            items.Add(item);
        next_row:
            ;
        }

        stationCode = stationCode ?? throw new InvalidOperationException("Station code is missing");
        return new MeasurementReport()
        {
            Code = stationCode,
            LocationName = locationName ?? throw new InvalidOperationException("Location name is missing"),
            IsManualCollection = isManualCollection,
        };
    }

    public record ErrorRow(string SheetName, string CellReference, string MeasurementName, DateTime? MeasureDateTime);
}
