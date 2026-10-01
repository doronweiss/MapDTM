namespace MapDTM {
  public static class GeoUtils {
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
    public static List<(double Dist, double Lat, double Lon)> ResamplePath(IReadOnlyList<PointData> path, double step) {
      var result = new List<(double Dist, double Lat, double Lon)>();
      if (path.Count == 0)
        return result;

      double d = 0;
      for (int i = 0; i < path.Count - 1; i++) {
        var a = path[i];
        var b = path[i + 1];
        double len = b.Dist - a.Dist;
        if (len <= 0)
          continue;
        for (; d < b.Dist; d += step) {
          double t = (d - a.Dist) / len;
          result.Add((d, a.Lat + t * (b.Lat - a.Lat), a.Lon + t * (b.Lon - a.Lon)));
        }
      }

      var last = path[^1];
      result.Add((last.Dist, last.Lat, last.Lon));
      return result;
    }

    static double ToRad(double deg) => deg * Math.PI / 180.0;
  }
}
