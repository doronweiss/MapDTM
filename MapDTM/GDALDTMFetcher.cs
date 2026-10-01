using OSGeo.GDAL;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapDTM;

internal class GeoTiffTransform {
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

internal class GDALDTMFetcher {
  Dataset? dataset = null;
  GeoTiffTransform? gtt = null;


  public bool Init(string sourceFilePath) {
    try {
      dataset = Gdal.Open(sourceFilePath, Access.GA_ReadOnly);
      if (dataset == null) {
        Console.WriteLine($"Failed to open dataset: {sourceFilePath}");
        return false;
      }
      gtt = GeoTiffTransform.FromDataset(dataset);
      return true;
    } catch (Exception ex) {
      Console.WriteLine($"Exception while opening dataset: {ex.Message}");
      return false;
    }
  }

  public double? GetAltitude(double lat, double lon) {
    if (dataset == null || gtt == null)
      return null;
    // Check if the point is within the bounds of the dataset
    if (lon < gtt.topLeftLongitude || lon > gtt.bottomRightLongitude ||
        lat > gtt.topLeftLatitude || lat < gtt.bottomRightLatitude) {
      return null; // Point is outside the bounds
    }
    // Calculate pixel coordinates
    int pixelX = (int)((lon - gtt.topLeftLongitude) / gtt.pixelWidth);
    int pixelY = (int)((gtt.topLeftLatitude - lat) / Math.Abs(gtt.pixelHeight));
    // Read the altitude value from the dataset
    Band band = dataset.GetRasterBand(1); // Assuming single-band DTM
    float[] buffer = new float[1];
    band.ReadRaster(pixelX, pixelY, 1, 1, buffer, 1, 1, 0, 0);
    return buffer[0];
  }
}