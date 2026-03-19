using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using LightInsight.Dashboard.Dashboard;

namespace LightInsight.Dashboard.Camera.Client
{
    public partial class CameraOnlineWidget : System.Windows.Controls.UserControl, IResizableWidget 
	//, IDisposable
    {
        public event EventHandler DeleteRequested;
        public int MinCol => 2;
        public int MinRow => 2;
        public Thumb ResizeThumb => this.InternalResizeThumb;
		private readonly CameraServices _cServices;
        public CameraOnlineWidget()
        {
            InitializeComponent();
            DeleteButton.Visibility = Visibility.Collapsed;
			_cServices = new CameraServices();
			_cServices.StatusUpdated += (online, offline, totalCount) => {
				TxtOnlineCount.Text = online.ToString();
			};
            _cServices.Start();

			TestApiButton_Click();

			this.Unloaded += (s, e) => {
				_cServices?.Dispose();
			};
		}

        public void SetEditMode(bool isEdit)
        {
            DeleteButton.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
            this.Cursor = isEdit ? System.Windows.Input.Cursors.SizeAll : System.Windows.Input.Cursors.Arrow;
        }
        private void DeleteWidget_Click(object sender, RoutedEventArgs e)
        {
            DeleteRequested?.Invoke(this, EventArgs.Empty);
			_cServices?.Dispose();
		}

		private async void TestApiButton_Click()
		{
			var myApi = new api();

			// 1. Test lấy Token thô
			myApi.GetMilestoneAccessToken();

			// 2. Test gọi REST API (Asynchronous)
			await myApi.TestRestApiCall();

		}
	}
}