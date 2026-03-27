using ServiceDAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceBUS
{
    public class CameraServiceBUS
    {
        private readonly CameraService _cameraService;
        public CameraServiceBUS() 
        {
            _cameraService = new CameraService();
        }
        public Dictionary<string, string> LoadCameraUriMap()
        {
            return _cameraService.LoadCameraUriMap();
        }
    }
}
