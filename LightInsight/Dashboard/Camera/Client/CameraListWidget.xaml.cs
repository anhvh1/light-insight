using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LightInsight.Dashboard.Dashboard;
using System.Windows.Controls.Primitives;
using VideoOS.Platform;
using VideoOS.Platform.Client;
using VideoOS.Platform.Messaging;
using System.Threading;


namespace LightInsight.Dashboard.Camera.Client
{
    /// <summary>
    /// Interaction logic for CameraListWidget.xaml
    /// </summary>
    public class CameraInfo
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public string StatusColor { get; set; } // Màu cho Border Status
        public string IP { get; set; }
        public string Recording { get; set; }
        public string Uptime { get; set; }
    }
    public class PageItem
    {
        public string Content { get; set; }
        public bool IsSelected { get; set; }
        // Ghi đè ToString để Button hiển thị đúng số
        public override string ToString() => Content;
    }

    public partial class CameraListWidget : UserControl, IResizableWidget, IDisposable
    {
        private ResourceDictionary _currentThemeDictionary;
        private object _themeChangedRegistration;
        public int MinCol => 6;
        public int MinRow => 4;
        public Thumb ResizeThumb => this.InternalResizeThumb;

        public event EventHandler DeleteRequested;
        private List<CameraInfo> _allCameras;
        public int _currentPage = 1;
        private const int MaxPages = 2;
        private CameraServices _cameraServices;

        public CameraListWidget()
        {
            ApplySmartClientLanguage(Thread.CurrentThread.CurrentUICulture.Name);
            InitializeComponent();
            ApplySmartClientTheme(ClientControl.Instance?.Theme);
            _themeChangedRegistration = EnvironmentManager.Instance.RegisterReceiver(
                new MessageReceiver(OnThemeChanged),
                new MessageIdFilter(MessageId.SmartClient.ThemeChangedIndication));
            DeleteButton.Visibility = Visibility.Collapsed;

            _cameraServices = new CameraServices();
            _cameraServices.Start();
			_cameraServices.StatusUpdated += (on, off, total) => {
                Dispatcher.Invoke(() =>
                {
                    _allCameras = _cameraServices.GetCameraList();
                    UpdateTable(_currentPage.ToString());
                });
            };

            LoadRealData();

            // Thay vì gọi trực tiếp, hãy đợi Widget load xong kích thước
            this.Loaded += (s, e) => {
                UpdateTable("1");
            };
        }

        public void Dispose()
        {
            if (_cameraServices != null)
            {
                _cameraServices.Dispose();
            }
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
        public void SetEditMode(bool isEdit)
        {
            DeleteButton.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
            if (InternalResizeThumb != null)
                InternalResizeThumb.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;

            var mainBorder = FindName("MainBorder") as Border;
            if (mainBorder != null)
            {
                if (isEdit)
                {
                    if (mainBorder.Tag is System.Windows.Media.Brush originalBorderBrush)
                        mainBorder.BorderBrush = originalBorderBrush;
                    mainBorder.BorderThickness = new Thickness(1);
                }
                else
                {
                    if (!(mainBorder.Tag is System.Windows.Media.Brush))
                        mainBorder.Tag = mainBorder.BorderBrush;
                    mainBorder.BorderBrush = TryFindResource("CardBorder") as System.Windows.Media.Brush
                        ?? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60));
                    mainBorder.BorderThickness = new Thickness(1);
                }
            }
            this.Cursor = isEdit ? Cursors.SizeAll : Cursors.Arrow;
        }
        private void DeleteWidget_Click(object sender, RoutedEventArgs e)
        {
            DeleteRequested?.Invoke(this, EventArgs.Empty);
        }

        private void LoadRealData()
        {
            if (_cameraServices != null)
            {
                _allCameras = _cameraServices.GetCameraList();
            }
            else
            {
                _allCameras = new List<CameraInfo>();
            }
        }

        private int _totalPages = 1; // Thay cho MaxPages

        private string _currentStatusFilter = "All";

        private void Filter_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is RadioButton rb && rb.Tag != null)
                {
                    _currentStatusFilter = rb.Tag.ToString();
                    _currentPage = 1;
                    UpdateTable(_currentPage.ToString());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Filter_Checked Error]: {ex.Message} {ex.StackTrace}");
            }
        }

        public void UpdateTable(string page)
        {
            try
            {
                if (_allCameras == null) return;

                // 1. Lọc theo trạng thái
                var filteredData = _allCameras.AsEnumerable();
                if (_currentStatusFilter != "All")
                {
                    filteredData = filteredData.Where(c => c.Status == _currentStatusFilter);
                }

                // 2. Phân trang
                var list = filteredData.ToList();
                double usableHeight = this.ActualHeight > 0 ? this.ActualHeight : 300;
                int pageSize = (int)Math.Max(1, Math.Floor((usableHeight - 60) / 30));
                
                _totalPages = (int)Math.Ceiling((double)list.Count / pageSize);
                if (_totalPages < 1) _totalPages = 1;
                if (_currentPage > _totalPages) _currentPage = _totalPages;
                if (_currentPage < 1) _currentPage = 1;

                int pageNum = (page == ">") ? Math.Min(_currentPage + 1, _totalPages) :
                              (page == "<") ? Math.Max(_currentPage - 1, 1) :
                              int.TryParse(page, out int p) ? p : _currentPage;

                _currentPage = pageNum;

                var pagedData = list.Skip((_currentPage - 1) * pageSize).Take(pageSize).ToList();
                if (CameraDataGrid != null)
                    CameraDataGrid.ItemsSource = pagedData;

                UpdatePaginationUI(list.Count);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateTable Error]: {ex.Message} {ex.StackTrace}");
            }
        }
        private void PageButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            if (clickedButton == null) return;

            string content = clickedButton.Content.ToString();

            if (content == "<")
            {
                if (_currentPage > 1) _currentPage--;
            }
            else if (content == ">")
            {
                if (_currentPage < _totalPages) _currentPage++;
            }
            else
            {
                if (int.TryParse(content, out int targetPage))
                {
                    _currentPage = targetPage;
                }
            }

            UpdateTable(_currentPage.ToString());
        }

        private void UpdatePaginationUI(int filteredCount)
        {
            if (PaginationItemsControl == null) return;

            List<PageItem> pageButtons = new List<PageItem>();

            for (int i = 1; i <= _totalPages; i++)
            {
                pageButtons.Add(new PageItem
                {
                    Content = i.ToString(),
                    IsSelected = (i == _currentPage)
                });
            }

            // Đổ danh sách nút vào ItemsControl
            PaginationItemsControl.ItemsSource = pageButtons;

            // Cập nhật text Footer
            if (FooterText != null)
            {
                FooterText.Text = $"Showing {CameraDataGrid.Items.Count} of {filteredCount} (Page {_currentPage}/{_totalPages})";
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchBox.Text.ToLower();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                UpdateTable(_currentPage.ToString()); // Quay lại hiển thị theo trang nếu xóa hết chữ
            }
            else
            {
                // Lọc không phân biệt hoa thường
                var filteredData = _allCameras
                    .Where(c => c.Name.ToLower().Contains(searchText) || c.ID.ToLower().Contains(searchText))
                    .ToList();

                CameraDataGrid.ItemsSource = filteredData;
            }
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            // Chỉ thực hiện lọc khi người dùng nhấn Enter
            if (e.Key == Key.Enter)
            {
                ExecuteSearch();

                // Ngăn chặn tiếng "ting" của hệ thống khi nhấn Enter trong TextBox
                e.Handled = true;
            }
        }

        private void ExecuteSearch()
        {
            string searchText = SearchBox.Text.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Nếu ô search trống, quay về hiển thị phân trang như bình thường
                UpdateTable(_currentPage.ToString());
                return;
            }

            // Thực hiện lọc trên danh sách 1000 camera
            // Sử dụng LINQ để lọc theo Name hoặc ID
            var filteredData = _allCameras
                .Where(c => c.Name.ToLower().Contains(searchText) ||
                            c.ID.ToLower().Contains(searchText))
                .ToList();

            // Cập nhật giao diện
            CameraDataGrid.ItemsSource = filteredData;

            // (Tùy chọn) Cập nhật dòng text "Showing x of y" ở Footer để người dùng biết kết quả
            // FooterText.Text = $"Showing {filteredData.Count} of {_allCameras.Count}";

            System.Diagnostics.Debug.WriteLine($"Đã tìm thấy {filteredData.Count} kết quả cho: {searchText}");
        }
        private void SearchIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ExecuteSearch();
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
            });
        }

    }
}
