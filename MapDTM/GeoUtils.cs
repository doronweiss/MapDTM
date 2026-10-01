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

    static double ToRad(double deg) => deg * Math.PI / 180.0;
  }
}
