using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives; // Thêm thư viện này cho Thumb
using System.Windows.Media;

namespace LightInsight.Dashboard.Camera.Client
{
    public partial class temp : UserControl
    {
        public event EventHandler DeleteRequested;

        public temp()
        {
            InitializeComponent();
            DeleteButton.Visibility = Visibility.Collapsed;

            // 1. Logic mặc định chiếm 1/2 chiều dài khung chứa khi load
            this.Loaded += (s, e) =>
            {
                var parent = VisualTreeHelper.GetParent(this) as FrameworkElement;
                if (parent != null)
                {
                    this.Width = parent.ActualWidth / 2;
                }
            };
        }

        public void SetEditMode(bool isEdit)
        {
            DeleteButton.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
            // Hiện điểm kéo khi ở chế độ edit (tùy chọn)
            ResizeThumb.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
        }

        private void DeleteWidget_Click(object sender, RoutedEventArgs e)
        {
            DeleteRequested?.Invoke(this, EventArgs.Empty);
        }

        // 2. Logic kéo dãn kích thước
        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            // Thay đổi Width/Height dựa trên khoảng cách di chuyển của chuột
            double newWidth = this.ActualWidth + e.HorizontalChange;
            double newHeight = this.ActualHeight + e.VerticalChange;

            if (newWidth >= this.MinWidth) this.Width = newWidth;
            if (newHeight >= this.MinHeight) this.Height = newHeight;
        }
    }
}
