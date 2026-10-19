using System;
using System.Collections.Generic;
using System.Text;

namespace MapDTM;

internal class WayPoint {
  public int index = 0;
  public bool isStartPoint = false;
  public bool aboveMSL = false; // true - relative to sea level, false - relative to takeoff point altitude
  public int commandId = 0; // o	16: Waypoint.	21: Land.	22: Takeoff......
  public double param1 = 0.0; // varies by context
  public double param2 = 0.0; // Proximity threshold in meters to register the waypoint as reached (varies by context)
  public double param3 = 0.0; // Pass-through distance or circular orbit radius (varies by context)
  public double param4 = 0.0; // varies by context
  public double latitude = 0.0; // degrees
  public double longitude = 0.0; // degrees
  public double altitude = 0.0; // meters
  public bool autoContinue = false;

  public WayPoint Clone() {
    return new WayPoint() {
      index = index, isStartPoint = isStartPoint, aboveMSL = aboveMSL, commandId = commandId,
      param1 = param1, param2 = param2, param3 = param3, param4 = param4,
      latitude = latitude, longitude = longitude, altitude = altitude,
      autoContinue = autoContinue
    };
  }

  /// <summary>
  /// Parses one MAVLink waypoint line (QGC WPL 110 layout, tab or space separated):
  /// index current frame command p1 p2 p3 p4 lat lon alt autocontinue.
  /// Returns null for headers, comments and malformed lines.
  /// </summary>
  public static WayPoint? FromMavlinkWP(string line) {
    var f = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
    if (f.Length < 12)
      return null;
    var inv = System.Globalization.CultureInfo.InvariantCulture;
    var d = new double[12];
    for (int i = 0; i < 12; i++)
      if (!double.TryParse(f[i], System.Globalization.NumberStyles.Float, inv, out d[i]))
        return null;
    return new WayPoint() {
      index = (int)d[0], isStartPoint = d[1] != 0, aboveMSL = d[2] == 0, commandId = (int)d[3],
      param1 = d[4], param2 = d[5], param3 = d[6], param4 = d[7],
      latitude = d[8], longitude = d[9], altitude = d[10],
      autoContinue = d[11] == 1
    };
  }

  /// <summary>
  /// Formats the waypoint as one QGC WPL 110 line (tab separated, invariant culture).
  /// </summary>
  public string MakeMavlinkWP() {
    var inv = System.Globalization.CultureInfo.InvariantCulture;
    return string.Join("\t",
      index.ToString(inv),
      isStartPoint ? "1" : "0",
      aboveMSL ? "0" : "3",
      commandId.ToString(inv),
      param1.ToString("F8", inv),
      param2.ToString("F8", inv),
      param3.ToString("F8", inv),
      param4.ToString("F8", inv),
      latitude.ToString("F8", inv),
      longitude.ToString("F8", inv),
      altitude.ToString("F6", inv),
      autoContinue ? "1" : "20");
  }
}