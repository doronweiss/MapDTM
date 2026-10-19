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

  public string MakeMavlinkWP() {
    StringBuilder sb = new();
    sb.Append($"{index} ");
    sb.Append($"{(isStartPoint ? 1 : 0)} ");
    sb.Append($"{(aboveMSL ? 0 : 3)} ");
    sb.Append($"{commandId} ");
    sb.Append($"{param1:F6} ");
    sb.Append($"{param2:F6} ");
    sb.Append($"{param3:F6} ");
    sb.Append($"{param4:F6} ");
    sb.Append($"{latitude:F6} ");
    sb.Append($"{longitude:F6} ");
    sb.Append($"{altitude:F6} ");
    sb.Append($"{(autoContinue ? 1 : 20)}");
    return sb.ToString();
  }
}