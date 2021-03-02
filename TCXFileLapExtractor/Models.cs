using System;
using System.Collections.Generic;

namespace TCXFileLapExtractor
{
    public class Coordinate
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public Coordinate(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }

    public class CRS
    {
        public string type { get; set; }
        public Properties properties { get; set; }
    }

    public class ElevationPoint
    {
        public double MilePosition { get; set; }
        public double ElevationInFeet { get; set; }
    }

    public class Feature
    {
        public string type { get; set; }
        public Dictionary<string, string> properties { get; set; }
        public Geometry geometry { get; set; }
    }

    public class GeoJsonFile
    {
        public string type { get; set; }
        public string name { get; set; }
        public CRS crs { get; set; }
        public List<Feature> features { get; set; }
    }

    public class Instant
    {
        public bool Start { get; set; }
        public int Index { get; set; }
        public int FeatureId { get; set; }
        public Coordinate Coordinate { get; set; }
        public float Elevation { get; set; }
        public DateTime Time { get; set; }
    }

    public class Properties
    {
        public string name { get; set; }
    }

    public class Point
    {
        public Dictionary<string, string> properties { get; set; }
        public Coordinate Coordinate { get; set; }
        public DateTime DateTime { get; set; }
        public double Elevation { get; set; }
    }

    public class PointFeature
    {
        public string type { get; set; }
        public Dictionary<string, string> properties { get; set; }
        public PointGeometry geometry { get; set; }
    }

    public class Geometry
    {
        public string type { get; set; }
        public List<List<double>> coordinates { get; set; }
    }

    public class PointGeoJsonFile
    {
        public string type { get; set; }
        public string name { get; set; }
        public CRS crs { get; set; }
        public List<PointFeature> features { get; set; }
    }

    public class PointGeometry
    {
        public string type { get; set; }
        public List<double> coordinates { get; set; }
    }

}