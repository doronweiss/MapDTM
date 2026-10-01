using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using MaxRev.Gdal.Core;
using ZedGraph;

namespace MapDTM;

// time an operation
public class OpTimer {
  private DateTime opStartDT;
  private int counter = 0;

  public OpTimer() =>
    opStartDT = DateTime.UtcNow;

  public void Reset() =>
    opStartDT = DateTime.UtcNow;

  public void Inc() => counter++;

  public double Elapsed =>
    (DateTime.UtcNow - opStartDT).TotalMilliseconds;

  public (double, double, int) ElapsedEx() =>
    counter == 0
      ? ((DateTime.UtcNow - opStartDT).TotalMilliseconds, 0.0, 0)
      : ((DateTime.UtcNow - opStartDT).TotalMilliseconds, ((DateTime.UtcNow - opStartDT).TotalMilliseconds / counter), counter);
}
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
  const string MapHost = "mapdtm.local";
  const double ProfileStep = 10; // profile sampling interval [m]
  private GDALDTMFetcher? gdalDTM;

  /// <summary>Last calculated profile: altitude [m] every ProfileStep meters along the path. NaN where the DTM has no data.</summary>
  List<(double Dist, double Alt)> profile = new();

  /// <summary>Points picked on the map, in click order.</summary>
  public List<PointData> Points { get; } = new();

  public MainWindow() {
    InitializeComponent();
    PointsGrid.ItemsSource = Points;
    InitGraph();
    Loaded += async (_, _) => await InitMapAsync();
  }

  private void OnWindowLoaded(object sender, RoutedEventArgs e) {
    string onedriveFolder = Environment.GetEnvironmentVariable("OneDrive");
    string filePath = Path.Combine(onedriveFolder, @"Projects\DTM\israel_hh.tif");
    GdalBase.ConfigureAll(); // MaxRev.Gdal: load native GDAL and register drivers before any Gdal call
    gdalDTM = new GDALDTMFetcher();
    if (!gdalDTM.Init(filePath)) {
      MessageBox.Show("Failed to initialize GDAL DTM fetcher.");
    }
  }
  #region Map
  async Task InitMapAsync() {
    try {
      string userDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MapDTM", "WebView2");
      var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
      await MapView.EnsureCoreWebView2Async(env);

      string webFolder = Path.Combine(AppContext.BaseDirectory, "Web");
      MapView.CoreWebView2.SetVirtualHostNameToFolderMapping(MapHost, webFolder, CoreWebView2HostResourceAccessKind.Allow);
      MapView.CoreWebView2.WebMessageReceived += MapView_WebMessageReceived;
      MapView.CoreWebView2.Navigate($"https://{MapHost}/map.html");
      SetStatus("Loading map...");
    }
    catch (Exception ex) {
      System.Windows.MessageBox.Show(this, "Failed to initialize the map (is the WebView2 runtime installed?)\n\n" + ex.Message,
        "MapDTM", MessageBoxButton.OK, MessageBoxImage.Error);
    }
  }

  void MapView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e) {
    using var doc = JsonDocument.Parse(e.WebMessageAsJson);
    var root = doc.RootElement;
    switch (root.GetProperty("type").GetString()) {
      case "ready":
        SetStatus("Click the map to add points.");
        break;
      case "click":
        AddPoint(root.GetProperty("lat").GetDouble(), root.GetProperty("lon").GetDouble());
        break;
    }
  }

  Task RunMapScript(string script) {
    if (MapView.CoreWebView2 == null)
      return Task.CompletedTask;
    return MapView.CoreWebView2.ExecuteScriptAsync(script);
  }

  static string Js(double v) => v.ToString("R", CultureInfo.InvariantCulture);

  #endregion

  #region Points
  void AddPoint(double lat, double lon) {
    double dist = 0;
    if (Points.Count > 0) {
      var last = Points[^1];
      dist = last.Dist + GeoUtils.Distance(last.Lat, last.Lon, lat, lon);
    }
    Points.Add(new PointData(dist, lat, lon));
    PointsGrid.Items.Refresh();
    PointsGrid.ScrollIntoView(Points[^1]);

    _ = RunMapScript($"addPoint({Js(lat)}, {Js(lon)}, {Points.Count})");
    SetStatus($"{Points.Count} point(s), total {Points[^1].Dist:F0} m.");
  }

  void UndoButton_Click(object sender, RoutedEventArgs e) {
    if (Points.Count == 0)
      return;
    Points.RemoveAt(Points.Count - 1);
    PointsGrid.Items.Refresh();
    _ = RunMapScript("removeLastPoint()");
    profile.Clear();
    PlotProfile();
    SetStatus($"{Points.Count} point(s).");
  }

  void ClearButton_Click(object sender, RoutedEventArgs e) {
    Points.Clear();
    PointsGrid.Items.Refresh();
    _ = RunMapScript("clearPoints()");
    profile.Clear();
    PlotProfile();
    SetStatus("Cleared. Click the map to add points.");
  }

  async void CalculateButton_Click(object sender, RoutedEventArgs e) {
    if (Points.Count < 2) {
      SetStatus("Add at least 2 points before calculating.");
      return;
    }
    var dtm = gdalDTM;
    if (dtm == null) {
      SetStatus("DTM is not loaded.");
      return;
    }

    CalculateButton.IsEnabled = false;
    SetStatus("Calculating profile...");
    OpTimer ot = new OpTimer();
    double time = 0.0;
    int pcnt = 0;
    try {
      var pts = Points.ToList();
      var (newProfile, pointAlts) = await Task.Run(() => {
        var samples = GeoUtils.ResamplePath(pts, ProfileStep);
        var prof = samples.Select(s => (s.Dist, dtm.GetAltitude(s.Lat, s.Lon) ?? double.NaN)).ToList();
        var alts = pts.Select(p => dtm.GetAltitude(p.Lat, p.Lon)).ToArray();
        return (prof, alts);
      });

      for (int i = 0; i < pts.Count; i++)
        pts[i].Alt = pointAlts[i];
      profile = newProfile;

      PointsGrid.Items.Refresh();
      time = ot.Elapsed;
      pcnt = newProfile.Count;
      PlotProfile();
      int missing = profile.Count(p => double.IsNaN(p.Alt));
      SetStatus($"Profile: {profile.Count} samples every {ProfileStep} m" +
        (missing > 0 ? $", {missing} outside the DTM." : "."));
      MessageBox.Show(this, $"Calculation completed in {time:F0} ms for {pcnt} point(s).", "Calculate", MessageBoxButton.OK,
        MessageBoxImage.Information);
    }
    catch (Exception ex) {
      SetStatus("Calculation failed.");
      MessageBox.Show(this, ex.Message, "Calculate", MessageBoxButton.OK, MessageBoxImage.Error);
    }
    finally {
      CalculateButton.IsEnabled = true;
    }
  }

  void SetStatus(string text) => StatusText.Text = text;

  #endregion

  #region Graph

  void InitGraph() {
    var pane = ProfileGraph.GraphPane;
    pane.Title.Text = "Terrain Profile";
    pane.XAxis.Title.Text = "Distance [m]";
    pane.YAxis.Title.Text = "Altitude [m]";
    pane.XAxis.MajorGrid.IsVisible = true;
    pane.YAxis.MajorGrid.IsVisible = true;
    pane.Legend.IsVisible = false;
    ProfileGraph.IsShowPointValues = true;
    ProfileGraph.AxisChange();
  }

  void PlotProfile() {
    var pane = ProfileGraph.GraphPane;
    pane.CurveList.Clear();

    if (profile.Count > 0) {
      // Terrain profile; samples outside the DTM become gaps
      var terrain = new PointPairList();
      foreach (var (dist, alt) in profile)
        terrain.Add(dist, double.IsNaN(alt) ? PointPair.Missing : alt);
      var curve = pane.AddCurve("Altitude", terrain, System.Drawing.Color.SaddleBrown, SymbolType.None);
      curve.Line.Width = 2;
      curve.Line.Fill = new Fill(System.Drawing.Color.FromArgb(120, System.Drawing.Color.Peru));

      // Clicked points
      var clicked = new PointPairList();
      foreach (var p in Points.Where(p => p.Alt.HasValue))
        clicked.Add(p.Dist, p.Alt!.Value);
      var marks = pane.AddCurve("Points", clicked, System.Drawing.Color.Red, SymbolType.Circle);
      marks.Line.IsVisible = false;
      marks.Symbol.Size = 7;
      marks.Symbol.Fill = new Fill(System.Drawing.Color.White);
    }

    ProfileGraph.AxisChange();
    ProfileGraph.Invalidate();
  }

  #endregion

}