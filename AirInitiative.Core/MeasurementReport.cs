namespace AirInitiative.Core;

public class MeasurementReport
{
    public string Code { get; set; }
    public bool IsManualCollection { get; set; }
    public string LocationName { get; set; }
    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
    public string[] MeasurementTaken { get; set; }
    public MeasurementItem[] Measurements { get; set; }
}
