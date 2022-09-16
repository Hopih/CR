namespace OpenApiHttpClientExample;

public class WeatherData
{
    public List<Weather> Weather { get; set; }
    public string Base { get; set; }
    public Main Main { get; set; }
}

public class Main
{
    public double Temp { get; set; }
    public double FeelsLike { get; set; }
    public double TempMin { get; set; }
    public double TempMax { get; set; }
    public long Pressure { get; set; }
    public long Humidity { get; set; }
}

public class Weather
{
    public long Id { get; set; }
    public string Main { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; }
}