using MahApps.Metro.IconPacks;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Text.Json.Serialization;

namespace LightInsight.Dashboard.Dashboard.Workspace
{
    public class WorkspaceModel : INotifyPropertyChanged
    {
        public string Id { get; set; }

        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); OnPropertyChanged(nameof(DisplayTitle)); }
        }

        private PackIconMaterialKind _icon;
        public PackIconMaterialKind Icon
        {
            get => _icon;
            set { _icon = value; OnPropertyChanged(nameof(Icon)); }
        }

        public string Type { get; set; }
        public string ParentId { get; set; }
        public string Path { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        private bool _isGroup;
        [JsonIgnore]
        public bool IsGroup
        {
            get => _isGroup;
            set { _isGroup = value; OnPropertyChanged(nameof(IsGroup)); }
        }

        private bool _isExpanded;
        [JsonIgnore]
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(nameof(IsExpanded)); }
        }

        [JsonIgnore]
        public string DisplayTitle
        {
            get
            {
                if (string.IsNullOrEmpty(Name)) return string.Empty;
                if (Type == null)
                {
                    var localized = Application.Current.TryFindResource(Name) as string;
                    return localized ?? Name;
                }
                return Name;
            }
        }

        public ObservableCollection<WorkspaceModel> Children { get; set; } = new ObservableCollection<WorkspaceModel>();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void RefreshLocalization()
        {
            OnPropertyChanged(nameof(DisplayTitle));
            if (Children != null)
            {
                foreach (var child in Children) child.RefreshLocalization();
            }
        }
    }
}
