using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using System.IO;

namespace TCXFileLapExtractor
{
    public class PointCollector
    {
        public string Extension = ".tcx";
        public List<Point> PointsCollected { get; set; }
        public List<Instant> Instants { get; set; }
        public int FeatureId { get; set; }
        public double DuplicateThresholdDifference = 0.00008; // Point coordinates must have a difference greater than this value. Otherwise we assume a duplicate point.
        public bool RemoveDuplicatePoints = true;

        public void ProcessActivityFiles()
        {
            RemoveDuplicatePoints = true;
            ExtractPointsFromActivityFiles();
            RemoveDuplicates();
            CreateGeoJsonFiles();
            Console.WriteLine("Points collected: " + this.PointsCollected.Count);
        }

        private void ExtractPointsFromActivityFiles()
        {
            var files = Directory.GetFiles(".", "*" + this.Extension);
            this.PointsCollected = new List<Point>();
            this.Instants = new List<Instant>();
            this.FeatureId = 1;
            foreach (var file in files)
            {
                AddLapDataFromFile(new FileInfo(file));
            }
        }

        private void CreateGeoJsonFiles()
        {
            var pointGeoJson = CreatePointGeoJson();
            CreateGeoJsonFile("points.geojson", pointGeoJson);
            var lineGeoJson = CreateLineGeoJson();
            CreateGeoJsonFile("lines.geojson", lineGeoJson);
        }

        private void AddLapDataFromFile(FileInfo file)
        {
            var fileLines = GetFileLines(file);
            var laps = GetGroupedLines(fileLines, "Lap");
            if (laps.Count > 1)
            {
                AddInstantsFromLapLines(laps[0]);
                laps = laps.Skip(1).ToList(); // Do not count starting lap when collecting points.
            }
            foreach (var lapLines in laps)
            {
                var firstPoint = GetPointFromLapLines(lapLines, 0);
                if (firstPoint != null)
                {
                    this.PointsCollected.Add(firstPoint);
                }
                AddInstantsFromLapLines(lapLines);
                this.FeatureId++;
            }

        }

        private void AddInstantsFromLapLines(List<string> lapLines)
        {
            var trekPointGroups = GetGroupedLines(lapLines, "Trackpoint");
            foreach (var group in trekPointGroups)
            {
                var point = GetPointFromTrekPoint(group);
                var instant = GetInstantFromPoint(point);
                this.Instants.Add(instant);
            }
        }

        public Instant GetInstantFromPoint(Point point)
        {
            var instant = new Instant();
            instant.FeatureId = this.FeatureId;
            instant.Coordinate = point.Coordinate;
            instant.Elevation = (float)point.Elevation;
            return instant;
        }

        public List<string> GetFileLines(FileInfo file)
        {
            var filePath = file.Directory.FullName + "\\" + file.Name;
            var streamReader = new StreamReader(filePath);
            var fileContents = streamReader.ReadToEnd();
            return fileContents.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.None).ToList();
        }

        public Point GetPointFromLapLines(List<string> lines, int index = 0)
        {
            var trekPointGroups = GetGroupedLines(lines, "Trackpoint");
            if (trekPointGroups.Count > index)
            {
                var firstGroup = trekPointGroups[index];
                return GetPointFromTrekPoint(firstGroup);
            }
            return null;
        }

        public Point GetPointFromTrekPoint(List<string> lines)
        {
            var point = new Point();
            point.properties = new Dictionary<string, string>();
            var latString = GetTagValueFromLines(lines, "LatitudeDegrees");
            var longString = GetTagValueFromLines(lines, "LongitudeDegrees");
            point.Coordinate = new Coordinate(double.Parse(latString), double.Parse(longString));
            point.Elevation = float.Parse(GetTagValueFromLines(lines, "AltitudeMeters"));
            point.DateTime = DateTime.Parse(GetTagValueFromLines(lines, "Time"));
            point.properties.Add("latitude", latString);
            point.properties.Add("longitude", longString);
            point.properties.Add("date", point.DateTime.ToShortDateString());
            point.properties.Add("time", point.DateTime.ToShortTimeString());
            point.properties.Add("elevation", point.Elevation.ToString());
            return point;
        }

        public List<List<string>> GetGroupedLines(List<string> lines, string tag)
        {
            var startMarker = "<" + tag;
            var endMarker = "</" + tag + ">";
            var linesForThisGroup = new List<string>();
            var groups = new List<List<string>>();
            foreach (var line in lines)
            {
                if (line.Contains(startMarker))
                {
                    linesForThisGroup = new List<string>();
                }
                if (linesForThisGroup != null)
                {
                    linesForThisGroup.Add(line);
                }
                if (line.Contains(endMarker))
                {
                    groups.Add(linesForThisGroup);
                    linesForThisGroup = null;
                }
            }
            return groups;
        }

        public string GetTagValueFromLines(List<string> lines, string tag)
        {
            string result = null;
            foreach (var line in lines)
            {
                if (line.Contains("<" + tag + ">"))
                {
                    int indexOfStart = line.IndexOf(tag + ">") + (tag + ">").Length;
                    int indexOfEnd = line.IndexOf("</" + tag);
                    result = line.Substring(indexOfStart, indexOfEnd - indexOfStart);
                }
            }
            return result;
        }

        public void RemoveDuplicates()
        {
            if (RemoveDuplicatePoints)
            {
                var newList = new List<Point>();
                foreach (var point in this.PointsCollected)
                {
                    bool duplicateFound = false;
                    foreach (var addedPoints in newList)
                    {
                        var latDiff = Math.Abs(addedPoints.Coordinate.Latitude - point.Coordinate.Latitude);
                        var longDiff = Math.Abs(addedPoints.Coordinate.Longitude - point.Coordinate.Longitude);
                        if (latDiff <= this.DuplicateThresholdDifference && longDiff <= this.DuplicateThresholdDifference)
                        {
                            duplicateFound = true;
                        }
                    }
                    if (!duplicateFound)
                    {
                        newList.Add(point);
                    }
                }
                this.PointsCollected = newList;
            }
        }

        public string CreatePointGeoJson()
        {
            var geoJson = new PointGeoJsonFile();
            geoJson.type = "FeatureCollection";
            geoJson.features = new List<PointFeature>();
            geoJson.crs = new CRS();
            geoJson.crs.type = "name";
            geoJson.crs.properties = new Properties();
            geoJson.crs.properties.name = "urn:ogc:def:crs:OGC:1.3:CRS84";
            List<Point> instantsInThisFeature = new List<Point>();
            foreach (var point in PointsCollected)
            {
                var feature = GetNewPointFeature(point);
                geoJson.features.Add(feature);
            }
            return JsonSerializer.Serialize(geoJson);
        }

        public string CreateLineGeoJson()
        {
            var geoJson = new GeoJsonFile();
            geoJson.type = "FeatureCollection";
            geoJson.features = new List<Feature>();
            geoJson.crs = new CRS();
            geoJson.crs.type = "name";
            geoJson.crs.properties = new Properties();
            geoJson.crs.properties.name = "urn:ogc:def:crs:OGC:1.3:CRS84";

            int previousFeatureId = 0;
            List<List<double>> coordinates = new List<List<double>>();
            List<double> previousCoordinate = null;
            List<Instant> instantsInThisFeature = new List<Instant>();
            foreach (var instant in this.Instants)
            {
                var coordinate = new List<double> { instant.Coordinate.Longitude, instant.Coordinate.Latitude };
                if (previousFeatureId != 0 && previousFeatureId != instant.FeatureId)
                {
                    var feature = GetNewLineFeature(instant.FeatureId, coordinates);
                    if (feature != null || instant == Instants[Instants.Count - 1])
                    {
                        AddPropsToFeature(feature, instantsInThisFeature);
                    }
                    geoJson.features.Add(feature);
                    coordinates = new List<List<double>>();
                    instantsInThisFeature = new List<Instant>();
                }
                instantsInThisFeature.Add(instant);
                previousFeatureId = instant.FeatureId;
                previousCoordinate = coordinate;
                coordinates.Add(coordinate);
            }
            var lastFeature = GetNewLineFeature(previousFeatureId + 1, coordinates);
            AddPropsToFeature(lastFeature, instantsInThisFeature);
            geoJson.features.Add(lastFeature);
            return JsonSerializer.Serialize(geoJson);
        }

        private Feature GetNewLineFeature(int featureId, List<List<double>> coordinates)
        {
            var feature = new Feature();
            feature.type = "Feature";
            feature.properties = new Dictionary<string, string>();
            feature.properties.Add("featureId", featureId.ToString());
            var geometry = new Geometry();
            feature.geometry = geometry;
            geometry.type = "LineString";
            geometry.coordinates = coordinates;
            return feature;
        }

        private PointFeature GetNewPointFeature(Point point)
        {
            var feature = new PointFeature();
            var coordinate = new List<double> { point.Coordinate.Longitude, point.Coordinate.Latitude };
            feature.properties = new Dictionary<string, string>();
            feature.properties.Add("elevation", point.Elevation.ToString());
            feature.type = "Feature";
            feature.properties = point.properties;
            var geometry = new PointGeometry();
            feature.geometry = geometry;
            geometry.type = "Point";
            geometry.coordinates = coordinate;
            return feature;
        }

        private void AddPropsToFeature(Feature feature, List<Instant> instantsInThisFeature)
        {
            feature.properties.Add("numberOfSeconds", instantsInThisFeature.Count.ToString());
            var firstInstant = instantsInThisFeature[0];
            var lastInstant = instantsInThisFeature[instantsInThisFeature.Count - 1];
            feature.properties.Add("startingElevation", firstInstant.Elevation.ToString());
            feature.properties.Add("endingElevation", lastInstant.Elevation.ToString());
        }

        private void CreateGeoJsonFile(string filename, string geoJSON)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                sw.Write(geoJSON);
            }
        }

    }

}
