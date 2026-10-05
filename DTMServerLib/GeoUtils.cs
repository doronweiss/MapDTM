using OSGeo.GDAL;
using System;
using System.Collections.Generic;
using System.Text;

namespace DTMServerLib;

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
}


internal class GeoUtils {
}