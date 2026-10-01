using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using ZedGraph;

namespace MapDTM {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window {
    const string MapHost = "mapdtm.local";

    /// <summary>Points picked on the map, in click order.</summary>
    public List<PointData> Points { get; } = new();

    /// <summary>Altitude source. Replace the dummy with the real DTM library implementation.</summary>
    public IDtmProvider Dtm { get; set; } = new DummyDtmProvider();

    public MainWindow() {
      InitializeComponent();
      PointsGrid.ItemsSource = Points;
      InitGraph();
      Loaded += async (_, _) => await InitMapAsync();
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
      PlotProfile();
      SetStatus($"{Points.Count} point(s).");
    }

    void ClearButton_Click(object sender, RoutedEventArgs e) {
      Points.Clear();
      PointsGrid.Items.Refresh();
      _ = RunMapScript("clearPoints()");
      PlotProfile();
      SetStatus("Cleared. Click the map to add points.");
    }

    async void CalculateButton_Click(object sender, RoutedEventArgs e) {
      if (Points.Count < 2) {
        SetStatus("Add at least 2 points before calculating.");
        return;
      }

      CalculateButton.IsEnabled = false;
      SetStatus("Calculating altitudes...");
      try {
        var dtm = Dtm;
        var pts = Points.ToList();
        var alts = await Task.Run(() => pts.Select(p => dtm.GetAltitude(p.Lat, p.Lon)).ToArray());
        for (int i = 0; i < pts.Count; i++)
          pts[i].Alt = alts[i];

        PointsGrid.Items.Refresh();
        PlotProfile();
        SetStatus($"Calculated {pts.Count} altitude(s).");
      }
      catch (Exception ex) {
        SetStatus("Calculation failed.");
        System.Windows.MessageBox.Show(this, ex.Message, "Calculate", MessageBoxButton.OK, MessageBoxImage.Error);
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

      var list = new PointPairList();
      foreach (var p in Points.Where(p => p.Alt.HasValue))
        list.Add(p.Dist, p.Alt!.Value);

      if (list.Count > 0) {
        var curve = pane.AddCurve("Altitude", list, System.Drawing.Color.SaddleBrown, SymbolType.Circle);
        curve.Line.Width = 2;
        curve.Line.Fill = new Fill(System.Drawing.Color.FromArgb(120, System.Drawing.Color.Peru));
        curve.Symbol.Size = 5;
        curve.Symbol.Fill = new Fill(System.Drawing.Color.White);
      }

      ProfileGraph.AxisChange();
      ProfileGraph.Invalidate();
    }

    #endregion
  }
}
