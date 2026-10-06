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
        new GeoPoint() { latitude = 31.823198, longitude = 34.171192 }, new GeoPoint() { latitude = 31.84186, longitude = 35.22632 },
        new GeoPoint() { latitude = 31.242846, longitude = 35.907753 }, new GeoPoint() { latitude = 33.28569, longitude = 36.413334 },
        new GeoPoint() { latitude = 33.78003, longitude = 35.84181 }
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
