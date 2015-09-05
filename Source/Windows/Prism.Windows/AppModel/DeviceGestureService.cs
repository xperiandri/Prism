using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Input;

namespace Prism.Windows.AppModel
{
    public class DeviceGestureService : IDeviceGestureService
    {
        public bool IsHardwareBackButtonPresent
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsHardwareCameraButtonPresent
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsKeyboardPresent
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsMousePresent
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsTouchPresent
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool UseTitleBarBackButton
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public event EventHandler<DeviceGestureEventArgs> CameraButtonHalfPressed;
        public event EventHandler<DeviceGestureEventArgs> CameraButtonPressed;
        public event EventHandler<DeviceGestureEventArgs> CameraButtonReleased;
        public event EventHandler<DeviceGestureEventArgs> GoBackRequested;
        public event EventHandler<DeviceGestureEventArgs> GoForwardRequested;
        public event EventHandler<MouseEventArgs> MouseMoved;
    }
}
