using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LiveCharts.Wpf;
using LiveCharts;
using LightInsight.Dashboard.Dashboard;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using VideoOS.Platform;
using VideoOS.Platform.Client;
using VideoOS.Platform.Messaging;
using System.Threading;

namespace LightInsight.Dashboard.AlarmsAndEvents
{
    public partial class AlarmBySourceWidget : UserControl, IResizableWidget
    {
        private ResourceDictionary _currentThemeDictionary;
        private object _themeChangedRegistration;
        private bool _widgetEditMode;
        public event EventHandler DeleteRequested;

        private SeriesCollection _chartSeries = new SeriesCollection();
        private List<string> _yAxisLabels = new List<string>(); // Giờ là trục Y

        private SolidColorBrush _defaultColor = (SolidColorBrush)new BrushConverter().ConvertFrom("#E87E14");
        private SolidColorBrush _hoverColor = (SolidColorBrush)new BrushConverter().ConvertFrom("#F5A623");
        private System.Windows.Shapes.Rectangle _lastHoveredRect = null;

        public int MinCol => 4;

        public int MinRow => 3;

        public Thumb ResizeThumb => this.InternalResizeThumb;

        public AlarmBySourceWidget()
        {
            ApplySmartClientLanguage(Thread.CurrentThread.CurrentUICulture.Name);
            InitializeComponent();
            ApplySmartClientTheme(ClientControl.Instance?.Theme);
            _themeChangedRegistration = EnvironmentManager.Instance.RegisterReceiver(
                new MessageReceiver(OnThemeChanged),
                new MessageIdFilter(MessageId.SmartClient.ThemeChangedIndication));
            DeleteButton.Visibility = Visibility.Collapsed;

            LoadChartData();

            SourceChart.Series = _chartSeries;
            SourceAxisY.Labels = _yAxisLabels; // Ép nhãn vào trục Y
        }
        private void ApplySmartClientLanguage(string name)
        {
            var uri = name == "vi-VN"
                       ? "/LightInsight;component/Dashboard/Dashboard/Language/Vi.xaml"
                       : "/LightInsight;component/Dashboard/Dashboard/Language/English.xaml";

            var dict = new ResourceDictionary
            {
                Source = new Uri(uri, UriKind.Relative)
            };

            Resources.MergedDictionaries.Clear();
            Resources.MergedDictionaries.Add(dict);
        }

        private void LoadChartData()
        {
            try
            {
                // 1. Gọi Service lấy TẤT CẢ danh sách Source
                List<SourceCountData> rawData = AlarmServices.GetAlarmCountsBySource();

                // Fallback nếu không có data
                if (rawData == null || rawData.Count == 0)
                {
                    rawData = new List<SourceCountData> { new SourceCountData { Source = "No Data", Count = 0 } };
                }

                // 2. Vẫn giữ nguyên lệnh đảo ngược để thanh dài nhất (Top 1) được đẩy lên trên cùng
                rawData.Reverse();

                var values = new ChartValues<int>();

                // 3. Đổ dữ liệu vào Chart và ngắt dòng thông minh theo chuẩn i-PRO
                foreach (var item in rawData)
                {
                    values.Add(item.Count);

                    string labelName = item.Source;

                    // Tìm vị trí của cụm " (" (Khoảng trắng + dấu ngoặc mở)
                    int bracketIndex = labelName.IndexOf(" (");

                    if (bracketIndex > 0)
                    {
                        // Cắt làm đôi, thay thế khoảng trắng đó bằng dấu xuống dòng \n
                        // Nửa đầu: lấy từ đầu đến trước khoảng trắng
                        // Nửa sau: lấy từ dấu ( trở đi
                        labelName = labelName.Substring(0, bracketIndex) + "\n" + labelName.Substring(bracketIndex + 1);
                    }
                    else if (labelName.Length > 30)
                    {
                        // DỰ PHÒNG: Lỡ có cái camera nào tên dài mà không có dấu (
                        // Thì tự động tìm khoảng trắng ở giữa để ngắt
                        int spaceIndex = labelName.IndexOf(' ', labelName.Length / 2);
                        if (spaceIndex < 0) spaceIndex = labelName.LastIndexOf(' ', labelName.Length / 2);

                        if (spaceIndex > 0)
                        {
                            labelName = labelName.Substring(0, spaceIndex) + "\n" + labelName.Substring(spaceIndex + 1);
                        }
                    }

                    _yAxisLabels.Add(labelName);
                }

                // VẼ THANH NGANG
                _chartSeries.Add(new RowSeries
                {
                    Values = values,
                    Fill = _defaultColor,

                    // Tác giả LiveCharts gõ sai chính tả: MaxRowHeigth
                    MaxRowHeigth = 40,

                    RowPadding = 4,
                    StrokeThickness = 0
                });
            }
            catch (Exception ex)
            {
                EnvironmentManager.Instance.Log(false, "LightInsight Widget", "LoadChart Source Error: " + ex.Message);
            }
        }

        private void Chart_DataHover(object sender, ChartPoint chartPoint)
        {
            RestoreLastHoveredPoint();
            if (chartPoint == null) return;

            // Tuyệt chiêu Reflection vẫn hoạt động tốt với RowSeries
            var view = chartPoint.View;
            if (view != null)
            {
                var rectInfo = view.GetType().GetProperty("Rectangle");
                if (rectInfo != null)
                {
                    _lastHoveredRect = rectInfo.GetValue(view) as System.Windows.Shapes.Rectangle;
                    if (_lastHoveredRect != null)
                    {
                        _lastHoveredRect.Fill = _hoverColor;
                    }
                }
            }

            // Mở Popup: Chú ý bây giờ Index nằm ở trục Y, còn Value nằm ở trục X
            int index = (int)chartPoint.Y;
            if (index >= 0 && index < _yAxisLabels.Count)
            {
                TooltipSource.Text = _yAxisLabels[index];
                TooltipCount.Text = $"count : {chartPoint.X}"; // Lấy giá trị từ trục X
                HoverPopup.IsOpen = true;
            }
        }

        private void Chart_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            RestoreLastHoveredPoint();
            HoverPopup.IsOpen = false;
        }

        private void RestoreLastHoveredPoint()
        {
            if (_lastHoveredRect != null)
            {
                _lastHoveredRect.Fill = _defaultColor;
                _lastHoveredRect = null;
            }
        }

        private object OnThemeChanged(Message message, FQID dest, FQID sender)
        {
            var theme = message?.Data as Theme;
            ApplySmartClientTheme(theme);
            return null;
        }

        private void ApplySmartClientTheme(Theme scTheme)
        {
            Dispatcher.Invoke(() =>
            {
                var themeUri = "/LightInsight;component/Dashboard/Dashboard/Themes/Dark.xaml";
                var crTheme = ClientControl.Instance.Theme.ThemeType;
                if (crTheme == ThemeType.Light)
                    themeUri = "/LightInsight;component/Dashboard/Dashboard/Themes/Light.xaml";

                var newDict = new ResourceDictionary { Source = new Uri(themeUri, UriKind.RelativeOrAbsolute) };

                if (_currentThemeDictionary != null)
                    Resources.MergedDictionaries.Remove(_currentThemeDictionary);

                Resources.MergedDictionaries.Insert(0, newDict);
                _currentThemeDictionary = newDict;
                DashboardWidgetChrome.SyncMainBorderBrush(this, _widgetEditMode);
            });
        }

        public void SetEditMode(bool isEdit)
        {
            _widgetEditMode = isEdit;
            DeleteButton.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
            if (InternalResizeThumb != null)
                InternalResizeThumb.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;

            DashboardWidgetChrome.SyncMainBorderBrush(this, _widgetEditMode);
            this.Cursor = isEdit ? Cursors.SizeAll : Cursors.Arrow;
        }

        private void DeleteWidget_Click(object sender, RoutedEventArgs e)
        {
            DeleteRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}