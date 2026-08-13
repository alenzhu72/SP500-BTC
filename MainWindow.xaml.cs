using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace MarketFloat;

public partial class MainWindow : Window
{
    private readonly MarketDataService _data = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMinutes(2) };
    private readonly Forms.NotifyIcon _tray;
    private CancellationTokenSource? _refreshCts;
    private bool _allowClose;
    private MarketRange _range = MarketRange.Day;
    private bool _ready;

    public MainWindow()
    {
        InitializeComponent();
        Left = SystemParameters.WorkArea.Right - Width - 24;
        Top = SystemParameters.WorkArea.Top + 24;

        _tray = new Forms.NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "MarketFloat - 市场悬浮窗",
            Visible = true,
            ContextMenuStrip = BuildTrayMenu()
        };
        _tray.DoubleClick += (_, _) => ShowWidget();
        _timer.Tick += async (_, _) => await RefreshAsync();
        Loaded += async (_, _) => { _ready = true; _timer.Start(); await RefreshAsync(); };
    }

    private Forms.ContextMenuStrip BuildTrayMenu()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("显示悬浮窗", null, (_, _) => ShowWidget());
        menu.Items.Add("刷新行情", null, async (_, _) => await RefreshAsync());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ExitApp());
        return menu;
    }

    private async Task RefreshAsync()
    {
        _refreshCts?.Cancel();
        _refreshCts = new CancellationTokenSource();
        try
        {
            var token = _refreshCts.Token;
            var spTask = _data.LoadAsync("^GSPC", "S&P 500", _range, token);
            var btcTask = _data.LoadAsync("BTC-USD", "Bitcoin", _range, token);
            await Task.WhenAll(spTask, btcTask);
            Render(spTask.Result, SpPrice, SpChange, SpChart, "N2");
            Render(btcTask.Result, BtcPrice, BtcChange, BtcChart, "N0");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (SpChart.Values.Count == 0) SpPrice.Text = "暂时无法连接";
            if (BtcChart.Values.Count == 0) BtcPrice.Text = "暂时无法连接";
            SpChange.Text = BtcChange.Text = $"稍后自动重试 · {ex.Message}";
        }
    }

    private static void Render(MarketData data, System.Windows.Controls.TextBlock price,
        System.Windows.Controls.TextBlock change, Sparkline chart, string format)
    {
        var positive = data.Change >= 0;
        var color = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(positive ? "#58D68D" : "#FF6B7A"));
        price.Text = $"${data.Price.ToString(format)}";
        change.Text = $"{(positive ? "+" : "")}{data.Change.ToString(format)}  ({data.ChangePercent:+0.00;-0.00;0.00}%)";
        change.Foreground = color;
        chart.Stroke = color;
        chart.Values = data.Points;
        chart.ToolTip = $"最近更新：{data.UpdatedAt:HH:mm:ss}";
    }

    private void DragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAsync();
    private void Hide_Click(object sender, RoutedEventArgs e) => Hide();
    private void Exit_Click(object sender, RoutedEventArgs e) => ExitApp();

    private async void Range_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.RadioButton button ||
            !Enum.TryParse<MarketRange>(button.Tag?.ToString(), out var selected)) return;
        _range = selected;
        if (SpChart is not null) SpChart.Range = selected;
        if (BtcChart is not null) BtcChart.Range = selected;
        if (RangeCaption is not null)
            RangeCaption.Text = selected switch
            {
                MarketRange.Day => "过去1天 → 现在",
                MarketRange.Week => "过去1周 → 现在",
                MarketRange.Month => "过去1月 → 现在",
                MarketRange.Year => "过去1年 → 现在",
                _ => "至今"
            };
        if (_ready) await RefreshAsync();
    }

    private void ShowWidget()
    {
        Dispatcher.Invoke(() => { Show(); WindowState = WindowState.Normal; Activate(); Topmost = true; });
    }

    private void ExitApp()
    {
        _allowClose = true;
        _timer.Stop();
        _refreshCts?.Cancel();
        _tray.Visible = false;
        _tray.Dispose();
        Close();
        System.Windows.Application.Current.Shutdown();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_allowClose) { e.Cancel = true; Hide(); return; }
        base.OnClosing(e);
    }
}
