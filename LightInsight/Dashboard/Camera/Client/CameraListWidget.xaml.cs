using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LightInsight.Dashboard.Dashboard;
using System.Windows.Controls.Primitives;


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

	public partial class CameraListWidget : UserControl, IResizableWidget
	{
		public int MinCol => 6;
		public int MinRow => 4;
		public Thumb ResizeThumb => this.InternalResizeThumb;

		public event EventHandler DeleteRequested;
		private List<CameraInfo> _allCameras;
		public int _currentPage = 1;
		private const int MaxPages = 2;

		public CameraListWidget()
		{
			InitializeComponent();
			DeleteButton.Visibility = Visibility.Collapsed;
			LoadMockData();

			// Thay vì gọi trực tiếp, hãy đợi Widget load xong kích thước
			this.Loaded += (s, e) => {
				UpdateTable("1");
			};
		}
		public void SetEditMode(bool isEdit)
		{
			DeleteButton.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
            this.Cursor = isEdit ? Cursors.SizeAll : Cursors.Arrow;
        }
		private void DeleteWidget_Click(object sender, RoutedEventArgs e)
		{
			DeleteRequested?.Invoke(this, EventArgs.Empty);
		}

		private void LoadMockData()
		{
			_allCameras = new List<CameraInfo>();
			for (int i = 1; i <= 30; i++)
			{
				_allCameras.Add(new CameraInfo
				{
					ID = $"CAM-00{i}",
					Name = $"Camera Khu vực {i}",
					Status = i % 3 == 0 ? "Offline" : "Online",
					StatusColor = i % 3 == 0 ? "#4D1F1F" : "#1A3A26", // Đỏ đậm hoặc Xanh đậm
					IP = $"192.168.1.{100 + i}",
					Recording = "Yes",
					Uptime = "99.9%"
				});
			}
		}

		private int _totalPages = 1; // Thay cho MaxPages

		public void UpdateTable(string page)
		{
			if (_allCameras == null) return;

			// 1. Tính toán PageSize dựa trên chiều cao hiện tại
			int pageSize = (int)Math.Max(1, Math.Floor((this.ActualHeight - 60) / 30));

			// 2. Tính toán lại tổng số trang dựa trên PageSize mới
			_totalPages = (int)Math.Ceiling((double)_allCameras.Count / pageSize);

			// 3. Đảm bảo trang hiện tại không vượt quá tổng số trang mới
			if (_currentPage > _totalPages) _currentPage = _totalPages;
			if (_currentPage < 1) _currentPage = 1;

			int pageNum = (page == ">") ? _currentPage : int.Parse(page);

			// 4. Lấy dữ liệu
			var pagedData = _allCameras.Skip((pageNum - 1) * pageSize).Take(pageSize).ToList();
			CameraDataGrid.ItemsSource = pagedData;

			// 5. Cập nhật lại thanh điều hướng (Ẩn/hiện nút số tùy theo _totalPages)
			UpdatePaginationUI();
		}
		private void PageButton_Click(object sender, RoutedEventArgs e)
		{
			Button clickedButton = sender as Button;
			if (clickedButton == null) return;

			// Lấy nội dung (có thể là chuỗi "<", ">" hoặc đối tượng PageItem)
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
				// Nếu nhấn vào số trang
				if (int.TryParse(content, out int targetPage))
				{
					_currentPage = targetPage;
				}
			}

			UpdateTable(_currentPage.ToString());
			UpdatePaginationUI();
		}

		private void UpdatePaginationUI()
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
				FooterText.Text = $"Showing {CameraDataGrid.Items.Count} of {_allCameras.Count} (Page {_currentPage}/{_totalPages})";
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

	}
}
