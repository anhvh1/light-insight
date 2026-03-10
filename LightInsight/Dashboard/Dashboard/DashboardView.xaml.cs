using LightInsight.Dashboard.Camera.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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
using Path = System.IO.Path;

namespace LightInsight.Dashboard.Dashboard
{
    /// <summary>
    /// Interaction logic for DashboardView.xaml
    /// </summary>
    public partial class DashboardView : UserControl
    {
        bool editMode = false;
        private Point startPoint;
        Button currentMenu = null;
        string currentDashboard = "Operations";
        public DashboardView()
        {
            InitializeComponent();
            SelectMenu(OperationsBtn);
            LoadLayout();
        }
        private void Menu_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            SelectMenu(btn);
            currentDashboard = btn.Content.ToString();
            LoadLayout();
        }
        void SelectMenu(Button btn)
        {
            if (currentMenu != null)
                currentMenu.Background = Brushes.Transparent;

            btn.Background = Brushes.DodgerBlue;

            currentMenu = btn;
        }
        private void Widget_MouseDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(null);
        }


        private void EditLayoutBtn_Click(object sender, RoutedEventArgs e)
        {
            editMode = true;

            WidgetLibrary.Visibility = Visibility.Visible;

            EditLayoutBtn.Visibility = Visibility.Collapsed;
            SaveBtn.Visibility = Visibility.Visible;
            CancelBtn.Visibility = Visibility.Visible;

            foreach (var widget in DashboardGrid.Children.OfType<CameraOnlineWidget>())
            {
                widget.SetEditMode(true);
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            List<WidgetLayout> layouts = new List<WidgetLayout>();

            foreach (UIElement child in DashboardGrid.Children)
            {
                if (child is FrameworkElement widget)
                {
                    WidgetLayout layout = new WidgetLayout
                    {
                        Dashboard = currentDashboard,
                        Type = widget.GetType().Name,
                        X = Canvas.GetLeft(widget),
                        Y = Canvas.GetTop(widget)
                    };

                    layouts.Add(layout);
                }
            }

            SaveLayout(layouts);
            ExitEditMode();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            ExitEditMode();
        }

        private void ExitEditMode()
        {
            editMode = false;

            WidgetLibrary.Visibility = Visibility.Collapsed;

            EditLayoutBtn.Visibility = Visibility.Visible;
            SaveBtn.Visibility = Visibility.Collapsed;
            CancelBtn.Visibility = Visibility.Collapsed;

            foreach (var widget in DashboardGrid.Children.OfType<CameraOnlineWidget>())
            {
                widget.SetEditMode(false);
            }
        }
        private void DashboardGrid_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
        private void Widget_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(null);
            Vector diff = startPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                Button widget = sender as Button;

                if (widget == null) return;

                DragDrop.DoDragDrop(widget,
                    widget.Content.ToString(),
                    DragDropEffects.Copy);
            }
        }
        private void DashboardGrid_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(string)))
                return;

            string widgetName = e.Data.GetData(typeof(string)) as string;

            if (widgetName == "Camera Online Count")
            {
                // kiểm tra đã tồn tại chưa
                bool exists = DashboardGrid.Children
                    .OfType<CameraOnlineWidget>()
                    .Any();

                if (exists)
                {
                    MessageBox.Show("Widget này đã tồn tại trên dashboard!");
                    return;
                }

                var widget = new CameraOnlineWidget();
                widget.SetEditMode(editMode);
                widget.DeleteRequested += Widget_DeleteRequested;
                Point position = e.GetPosition(DashboardGrid);

                Canvas.SetLeft(widget, position.X);
                Canvas.SetTop(widget, position.Y);

                DashboardGrid.Children.Add(widget);
            }
        }
        private void Widget_DeleteRequested(object sender, EventArgs e)
        {
            if (!editMode)
                return;

            if (sender is FrameworkElement widget)
            {
                DashboardGrid.Children.Remove(widget);
            }
        }
       
        void SaveLayout(List<WidgetLayout> newLayouts)
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "LightInsight");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string filePath = Path.Combine(folder, "dashboard_layout.json");

            List<WidgetLayout> allLayouts = new List<WidgetLayout>();

            if (File.Exists(filePath))
            {
                string oldJson = File.ReadAllText(filePath);

                if (!string.IsNullOrWhiteSpace(oldJson))
                {
                    try
                    {
                        allLayouts = JsonSerializer.Deserialize<List<WidgetLayout>>(oldJson);
                    }
                    catch
                    {
                        allLayouts = new List<WidgetLayout>();
                    }
                }
            }

            allLayouts.RemoveAll(x => x.Dashboard == currentDashboard);

            allLayouts.AddRange(newLayouts);

            string json = JsonSerializer.Serialize(allLayouts, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(filePath, json);
        }
        void LoadLayout()
        {
            DashboardGrid.Children.Clear();

            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LightInsight");

            string filePath = Path.Combine(folder, "dashboard_layout.json");

            if (!File.Exists(filePath))
                return;

            string json = File.ReadAllText(filePath);

            if (string.IsNullOrWhiteSpace(json))
                return;

            List<WidgetLayout> layouts;

            try
            {
                layouts = JsonSerializer.Deserialize<List<WidgetLayout>>(json);
            }
            catch
            {
                return;
            }

            if (layouts == null)
                return;

            foreach (var layout in layouts)
            {
                // chỉ load layout của dashboard hiện tại
                if (layout.Dashboard != currentDashboard)
                    continue;

                FrameworkElement widget = null;

                if (layout.Type == "CameraOnlineWidget")
                    widget = new CameraOnlineWidget();

                if (widget != null)
                {
                    Canvas.SetLeft(widget, layout.X);
                    Canvas.SetTop(widget, layout.Y);

                    DashboardGrid.Children.Add(widget);
                }
            }
        }
    }
    public class WidgetLayout
    {
        public string Dashboard { get; set; }
        public string Type { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
    }
}
