using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LightInsight.Dashboard.Dashboard;
using System.Windows.Controls.Primitives;
using VideoOS.Platform;
using VideoOS.Platform.Client;
using VideoOS.Platform.Messaging;
using System.Threading;
namespace LightInsight.Dashboard.Camera.Client
{
	/// <summary>
	/// Interaction logic for CameraOnlineWidget.xaml
	/// </summary>
	public partial class Temp : UserControl, IResizableWidget
	{
		private ResourceDictionary _currentThemeDictionary;
		private object _themeChangedRegistration;
		public event EventHandler DeleteRequested;
		// Set minimum size of the Widget on the Dashboard Grid
		// default size can be set by the Tag attribute of the UserControl
		public int MinCol => 2;
		public int MinRow => 2;

		public Thumb ResizeThumb => this.InternalResizeThumb;
		public Temp()
		{
			ApplySmartClientLanguage(Thread.CurrentThread.CurrentUICulture.Name);
			InitializeComponent();
			ApplySmartClientTheme(ClientControl.Instance?.Theme);
			_themeChangedRegistration = EnvironmentManager.Instance.RegisterReceiver(
				new MessageReceiver(OnThemeChanged),
				new MessageIdFilter(MessageId.SmartClient.ThemeChangedIndication));
			DeleteButton.Visibility = Visibility.Collapsed;
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
		//public void SetEditMode(bool isEdit)
		//{
		//	DeleteButton.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
		//          this.Cursor = isEdit ? Cursors.SizeAll : Cursors.Arrow;
		//      }
		public void SetEditMode(bool isEdit)
		{
			// Hiện/Ẩn nút xóa
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
