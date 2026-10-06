using OSGeo.GDAL;
using System;
using System.Collections.Generic;
using System.Text;

namespace DTMServerLib;

public class GeoPoint {
  public double latitude;
  public double longitude;
  public double alt = 0.0;
  //public double dist = 0; // distance from path start
}

public class GeoTiffTransform {
  /* Transform Array Map:
     transform[0] = Top-Left X (Min Longitude)
     transform[1] = Pixel Width (Resolution)
     transform[2] = Row Rotation (Typically 0)
     transform[3] = Top-Left Y (Max Latitude)
     transform[4] = Column Rotation (Typically 0)
     transform[5] = Pixel Height (Resolution, Negative value)
  */
  public double topLeftLongitude;
  public double topLeftLatitude;
  public double bottomRightLongitude;
  public double bottomRightLatitude;
  public double pixelWidth;
  public double pixelHeight;
  public double rowRotation;
  public double columnRotation;
  public int widthInPixels;
  public int heightInPixels;

  public static GeoTiffTransform FromDataset(Dataset dataset) {
    GeoTiffTransform gtt = new GeoTiffTransform();
    double[] transform = new double[6];
    dataset.GetGeoTransform(transform);
    gtt.topLeftLongitude = transform[0];
    gtt.pixelWidth = transform[1];
    gtt.rowRotation = transform[2];
    gtt.topLeftLatitude = transform[3];
    gtt.columnRotation = transform[4];
    gtt.pixelHeight = transform[5];
    gtt.widthInPixels = dataset.RasterXSize;
    gtt.heightInPixels = dataset.RasterYSize;
    // calculated data
    gtt.bottomRightLongitude = gtt.topLeftLongitude + (gtt.widthInPixels * gtt.pixelWidth);
    gtt.bottomRightLatitude = gtt.topLeftLatitude + (gtt.heightInPixels * gtt.pixelHeight);
    //
    return gtt;
  }
}

public class GeoTiffDescriptor {
  public string fileName;
  public GeoTiffTransform transform;
  public Dataset? dataSet = null;

  public bool Contains(double latitude, double longitude) {
    return (latitude <= transform.topLeftLatitude && latitude >= transform.bottomRightLatitude &&
            longitude >= transform.topLeftLongitude && longitude <= transform.bottomRightLongitude);
  }

  public void Dispose() => dataSet?.Dispose();
}


internal class GeoUtils {
  const double EarthRadius = 6371008.8; // mean earth radius [m]

  /// <summary>
  /// Great-circle (haversine) distance in meters between two WGS84 points.
  /// </summary>
  public static double Distance(double lat1, double lon1, double lat2, double lon2) {
    double dLat = ToRad(lat2 - lat1);
    double dLon = ToRad(lon2 - lon1);
    double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
      Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
    return 2 * EarthRadius * Math.Asin(Math.Min(1, Math.Sqrt(a)));
  }

  /// <summary>
  /// Resamples a path every <paramref name="step"/> meters (measured along the path, using PointData.Dist).
  /// Positions are linearly interpolated in lat/lon within each segment, which is accurate enough for short segments.
  /// The last point of the path is always included.
  /// </summary>
  public static List<GeoPoint> ResamplePath(IReadOnlyList<GeoPoint> path, double step) {
    var result = new List<GeoPoint>();
    if (path.Count == 0)
      return result;

    double d = 0, dist = 0.0;
    for (int i = 0; i < path.Count - 1; i++) {
      var a = path[i];
      var b = path[i + 1];
      double len = Distance (b.latitude, b.longitude, a.latitude, a.longitude);
      dist += len;
      if (len <= 0)
        continue;
      for (; d < dist; d += step) {
        double t = (d - (dist - len)) / len;
        result.Add(new GeoPoint { latitude = a.latitude + t * (b.latitude - a.latitude), longitude = a.longitude + t * (b.longitude - a.longitude) });
      }
    }
    var last = path[^1];
    result.Add(new GeoPoint { latitude = last.latitude, longitude = last.longitude });
    return result;
  }

  static double ToRad(double deg) => deg * Math.PI / 180.0;
}