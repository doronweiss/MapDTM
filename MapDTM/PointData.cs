namespace MapDTM {
  /// <summary>
  /// A profile point picked on the map.
  /// </summary>
  public class PointData {
    /// <summary>Distance [m] from the first point, accumulated along the path (0 for the first point).</summary>
    public double Dist { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
    /// <summary>Altitude [m] from the DTM; null until Calculate is run.</summary>
    public double? Alt { get; set; }

    public PointData(double dist, double lat, double lon) {
      Dist = dist;
      Lat = lat;
      Lon = lon;
    }
  }
}
