using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

	public partial class CameraListWidget : UserControl
	{
		public event EventHandler DeleteRequested;
		private List<CameraInfo> _allCameras;
		private int _currentPage = 1;
		private const int MaxPages = 2;

		public CameraListWidget()
		{
			InitializeComponent();
			DeleteButton.Visibility = Visibility.Collapsed;
			// Tạo dữ liệu mẫu
			LoadMockData();

			// Mặc định hiển thị trang 1
			UpdateTable("1");
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
			for (int i = 1; i <= 8; i++)
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

		private void UpdateTable(string page)
		{
			if (page == ">") return; // Tạm thời bỏ qua nút chuyển nhanh

			int pageNum = int.Parse(page);
			int pageSize = 4; // Mỗi trang hiện 4 dòng

			// Lấy dữ liệu theo phân trang
			var pagedData = _allCameras.Skip((pageNum - 1) * pageSize).Take(pageSize).ToList();

			// Đổ dữ liệu vào bảng
			CameraDataGrid.ItemsSource = pagedData;
		}
		private void PageButton_Click(object sender, RoutedEventArgs e)
		{
			Button clickedButton = sender as Button;
			if (clickedButton == null) return;

			string content = clickedButton.Content.ToString();

			// Xử lý logic tính toán số trang
			if (content == "<")
			{
				if (_currentPage > 1) _currentPage--;
			}
			else if (content == ">")
			{
				if (_currentPage < MaxPages) _currentPage++;
			}
			else
			{
				_currentPage = int.Parse(content);
			}

			// Sau khi có số trang mới, cập nhật UI
			UpdatePaginationUI();
			UpdateTable(_currentPage.ToString());
		}

		private void UpdatePaginationUI()
		{
			var orangeBrush = (Brush)new BrushConverter().ConvertFrom("#E8751A");
			var darkBrush = (Brush)new BrushConverter().ConvertFrom("#3E3E42");

			// Reset màu cho tất cả các nút số
			BtnPage1.Background = darkBrush;
			BtnPage2.Background = darkBrush;

			// Tô màu cam cho nút số tương ứng với trang hiện tại
			if (_currentPage == 1) BtnPage1.Background = orangeBrush;
			else if (_currentPage == 2) BtnPage2.Background = orangeBrush;

			// In log kiểm tra
			System.Diagnostics.Debug.WriteLine($"Đang ở trang: {_currentPage}");
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
