namespace Ikon.App.Examples.Globe.DataModels;

public class GlobeDataPoint
{
    public float Latitude { get; set; }
    public float Longitude { get; set; }
    public float Magnitude { get; set; }
    public string? Label { get; set; }
}

public class GlobeDataSet
{
    public string SeriesName { get; set; } = "";
    public List<GlobeDataPoint> Points { get; set; } = [];
    public string Color { get; set; } = "#00ff00";
}
