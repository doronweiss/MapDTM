using DTMServerLib;

namespace DTMLibTester {
  internal class Program {
    static void Main(string[] args) {
      string onedriveFolder = Environment.GetEnvironmentVariable("OneDrive");
      string dtmFolder = Path.Combine(onedriveFolder, @"Projects\DTM");
      DTMServices dtmSvcs = new DTMServices();
      InitResult res = dtmSvcs.Init(dtmFolder);
      switch (res) {
        case InitResult.Success:
          Console.WriteLine("Initialization successful.");
          break;
        case InitResult.DirectoryNotFound:
          Console.WriteLine("Directory not found.");
          break;
        case InitResult.NoCatalog:
          bool tres = dtmSvcs.CreateCatalog();
          Console.WriteLine($"Catalog creation {(tres ? "successful" : "failed")}.");
          break;
        case InitResult.ErrorReadingCatalog:
          Console.WriteLine("Error reading catalog.");
          break;
        default:
          Console.WriteLine("Unknown result.");
          break;
      }
    }
  }
}
