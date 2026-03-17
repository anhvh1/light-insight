using MahApps.Metro.IconPacks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightInsight.Dashboard.Dashboard.Workspace
{
    public class WorkspaceModel : INotifyPropertyChanged
    {
        public string Id { get; set; }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        private PackIconMaterialKind _icon;
        public PackIconMaterialKind Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                OnPropertyChanged(nameof(Icon));
            }
        }

        public string Type { get; set; }

        public string ParentId { get; set; }

        public ObservableCollection<WorkspaceModel> Children { get; set; }
            = new ObservableCollection<WorkspaceModel>();

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}