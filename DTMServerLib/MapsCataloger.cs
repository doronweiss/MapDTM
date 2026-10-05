using System;
using System.Collections.Generic;
using System.Text;
using OSGeo.GDAL;

namespace DTMServerLib;
internal enum CatalogingResult {
  Success,
  DirectoryNotFound,
  NoGeoTiffFilesFound,
  ErrorReadingFile
}

internal class MapsCataloger {
  public (CatalogingResult, List<GeoTiffDescriptor>?) CreateCatalog(string directory) { 
    if (! Directory.Exists(directory)) {
      return (CatalogingResult.DirectoryNotFound, null);
    }
    string [] files = Directory.GetFiles(directory, "*.tif");
    if (files.Length == 0) {
      return (CatalogingResult.NoGeoTiffFilesFound, null);
    }
    List<GeoTiffDescriptor> descriptors = new List<GeoTiffDescriptor>();
    foreach (string file in files) {
      try {
        Dataset dataset = Gdal.Open(file, Access.GA_ReadOnly);
        if (dataset != null) {
          GeoTiffDescriptor descriptor = new GeoTiffDescriptor {
            fileName = file,
            transform = GeoTiffTransform.FromDataset(dataset)
          };
          descriptors.Add(descriptor);
          dataset.Dispose();
        }
      } catch {
        return (CatalogingResult.ErrorReadingFile, null);
      }
    }
    return (CatalogingResult.Success, descriptors);
  }
}