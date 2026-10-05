using MaxRev.Gdal.Core;
using Newtonsoft.Json;

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

  public bool CreateCatalog () {
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
