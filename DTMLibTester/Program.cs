using DTMServerLib;

namespace DTMLibTester {
  internal class Program {
    static void Main(string[] args) {
      bool res = false;
      string onedriveFolder = Environment.GetEnvironmentVariable("OneDrive");
      string dtmFolder = Path.Combine(onedriveFolder, @"Projects\DTM");
      DTMServices dtmSvcs = new DTMServices();
      // init
      InitResult initRes = dtmSvcs.Init(dtmFolder);
      switch (initRes) {
        case InitResult.Success:
          Console.WriteLine("Initialization successful.");
          break;
        case InitResult.DirectoryNotFound:
          Console.WriteLine("Directory not found.");
          break;
        case InitResult.NoCatalog:
          res = dtmSvcs.CreateCatalog();
          Console.WriteLine($"Catalog creation {(res ? "successful" : "failed")}.");
          break;
        case InitResult.ErrorReadingCatalog:
          Console.WriteLine("Error reading catalog.");
          break;
        default:
          Console.WriteLine("Unknown result.");
          break;
      }
      // load datasets
      List<GeoPoint> track = new List<GeoPoint> {
        new GeoPoint() { latitude = 31.723879, longitude = 34.45404 }, new GeoPoint() { latitude = 31.69585, longitude = 34.99809 },
        new GeoPoint() { latitude = 31.793913, longitude = 35.29484 }, new GeoPoint() { latitude = 31.831244, longitude = 35.553127 },
        new GeoPoint() { latitude = 31.583652, longitude = 35.49268 }, new GeoPoint() { latitude = 31.340092, longitude = 35.432228 },
        new GeoPoint() { latitude = 31.058275, longitude = 35.44322 }, new GeoPoint() { latitude = 31.344782, longitude = 35.68502 },
        new GeoPoint() { latitude = 31.952461, longitude = 35.89385 }
      };
      var (prepRes, count) = dtmSvcs.PrepFilesForPth(track);
      Console.WriteLine($"Preparation of files for path {(prepRes ? "successful" : "failed")}, {count} datasets loaded.");
      double [] altitudes = dtmSvcs.GetDTMData(track);
      for (int idx=0; idx< altitudes.Length; idx++) {
        Console.WriteLine($"Point {idx}: Latitude: {track[idx].latitude}, Longitude: {track[idx].longitude}, Altitude: {altitudes[idx]}");
      }
      dtmSvcs.UnInit();
    }
  }
}
