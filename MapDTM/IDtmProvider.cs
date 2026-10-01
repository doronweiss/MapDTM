namespace MapDTM {
  /// <summary>
  /// Altitude source. Implement this with the DTM library and assign it to MainWindow.Dtm.
  /// </summary>
  public interface IDtmProvider {
    /// <summary>Returns the terrain altitude [m] at the given WGS84 coordinate.</summary>
    double GetAltitude(double lat, double lon);
  }

  /// <summary>
  /// Placeholder until the real DTM library is plugged in. Returns synthetic (fake) terrain.
  /// </summary>
  public class DummyDtmProvider : IDtmProvider {
    public double GetAltitude(double lat, double lon) {
      return 300 + 150 * Math.Sin(lat * 200) * Math.Cos(lon * 150);
    }
  }
}
