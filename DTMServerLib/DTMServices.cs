using MaxRev.Gdal.Core;
using Newtonsoft.Json;
using OSGeo.GDAL;

namespace DTMServerLib;

public enum InitResult {
  Success,
  DirectoryNotFound,
  NoCatalog,
  ErrorReadingCatalog
}

public class DTMServices {

  private string tiffsFolder = "";
  private List<GeoTiffDescriptor> dtmCatalog;

  public InitResult Init(string directory) {
    tiffsFolder = directory;
    GdalBase.ConfigureAll(); // MaxRev.Gdal: load native GDAL and register drivers before any Gdal call
    if (!Directory.Exists(directory)) {
      return InitResult.DirectoryNotFound;
    }
    string catalogName = Path.Combine(directory, "dtmCatalog.json");
    if (!File.Exists(catalogName)) {
      return InitResult.NoCatalog;
    }
    try {
      string txt = File.ReadAllText(catalogName);
      dtmCatalog = JsonConvert.DeserializeObject<List<GeoTiffDescriptor>>(txt);
      return InitResult.Success;
    } catch {
      return InitResult.ErrorReadingCatalog;
    }
  }

  public void UnInit() {
    if (dtmCatalog == null)
      return;
    foreach (var descriptor in dtmCatalog) {
      descriptor.Dispose();
    }
  }

  public (bool, int) PrepFilesForPth(List<GeoPoint> track) {
    List<GeoPoint> resampled = GeoUtils.ResamplePath(track, 100.0);
    foreach (GeoPoint gp in resampled) {
      bool found = false;
      foreach (GeoTiffDescriptor descriptor in dtmCatalog) {
        if (descriptor.Contains(gp.latitude, gp.longitude)) {
          found = true;
          descriptor.dataSet ??= Gdal.Open(descriptor.fileName, Access.GA_ReadOnly);
          break;
        }
      }
      if (!found)
        return (false, 0);
    }
    return (true, dtmCatalog.Count(x => x.dataSet != null));
  }

  public double[] GetDTMData(List<GeoPoint> track) {
    double[] altitudes = new double[track.Count];
    for (int i = 0; i < track.Count; i++) {
      GeoPoint gp = track[i];
      foreach (GeoTiffDescriptor descriptor in dtmCatalog) {
        if (descriptor.Contains(gp.latitude, gp.longitude)) {
          altitudes[i] = descriptor.GetAltitude(gp.latitude, gp.longitude) ?? 0;
          break;
        }
      }
    }
    return altitudes;
  }

  public bool CreateCatalog() {
    if (!Directory.Exists(tiffsFolder)) {
      return false;
    }
    MapsCataloger cataloger = new MapsCataloger();
    var (result, descriptors) = cataloger.CreateCatalog(tiffsFolder);
    if (result != CatalogingResult.Success || descriptors == null) {
      return false;
    }
    dtmCatalog = descriptors;
    string catalogName = Path.Combine(tiffsFolder, "dtmCatalog.json");
    string json = JsonConvert.SerializeObject(dtmCatalog, Formatting.Indented);
    File.WriteAllText(catalogName, json);
    return true;
  }
}
