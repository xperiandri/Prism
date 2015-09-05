using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Input;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Prism.Windows.AppModel
{
    public class DeviceGestureService : IDeviceGestureService
    {
        public DeviceGestureService()
        {
            IsKeyboardPresent = new KeyboardCapabilities().KeyboardPresent != 0;
            IsMousePresent = new MouseCapabilities().MousePresent != 0;
            IsTouchPresent = new TouchCapabilities().TouchPresent != 0;

            if (IsMousePresent)
                MouseDevice.GetForCurrentView().MouseMoved += OnMouseMoved;

            Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated += OnAcceleratorKeyActivated;

            Window.Current.CoreWindow.PointerPressed += OnPointerPressed;
        }

        public bool IsHardwareBackButtonPresent => false;

        public bool IsHardwareCameraButtonPresent => false;

        public bool IsKeyboardPresent { get; }

        public bool IsMousePresent { get; }

        public bool IsTouchPresent { get; }

        public bool UseTitleBarBackButton
        {
            get { return false; }
            set { }
        }

        public event EventHandler<DeviceGestureEventArgs> CameraButtonHalfPressed;

        public event EventHandler<DeviceGestureEventArgs> CameraButtonPressed;

        public event EventHandler<DeviceGestureEventArgs> CameraButtonReleased;

        public event EventHandler<DeviceGestureEventArgs> GoBackRequested;

        public event EventHandler<DeviceGestureEventArgs> GoForwardRequested;

        public event EventHandler<MouseEventArgs> MouseMoved;

        /// <summary>
        /// Invokes the handlers attached to an eventhandler.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eventHandler">The EventHandler</param>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected void RaiseEvent<T>(EventHandler<T> eventHandler, object sender, T args)
        {
            EventHandler<T> handler = eventHandler;

            if (handler != null)
                foreach (EventHandler<T> del in handler.GetInvocationList())
                {
                    try
                    {
                        del(sender, args);
                    }
                    catch { } // Events should be fire and forget, subscriber fail should not affect publishing process
                }
        }

        /// <summary>
        /// Invokes the handlers attached to an eventhandler in reverse order and stops if a handler has canceled the event.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eventHandler"></param>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected void RaiseCancelableEvent<T>(EventHandler<T> eventHandler, object sender, T args) where T : CancelEventArgs
        {
            EventHandler<T> handler = eventHandler;

            if (handler != null)
            {
                Delegate[] invocationList = handler.GetInvocationList();

                for (int i = invocationList.Length - 1; i >= 0; i--)
                {
                    EventHandler<T> del = (EventHandler<T>)invocationList[i];

                    try
                    {
                        del(sender, args);

                        if (args.Cancel)
                            break;
                    }
                    catch { } // Events should be fire and forget, subscriber fail should not affect publishing process
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected virtual void OnMouseMoved(MouseDevice sender, MouseEventArgs args)
        {
            RaiseEvent<MouseEventArgs>(MouseMoved, this, args);
        }

        /// <summary>
        /// Invoked on every keystroke, including system keys such as Alt key combinations.
        /// Used to detect keyboard navigation between pages.
        /// </summary>
        /// <param name="sender">Instance that triggered the event.</param>
        /// <param name="args">Event data describing the conditions that led to the event.</param>
        protected virtual void OnAcceleratorKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs args)
        {
            if ((args.EventType == CoreAcceleratorKeyEventType.SystemKeyDown ||
                args.EventType == CoreAcceleratorKeyEventType.KeyDown))
            {
                var coreWindow = Window.Current.CoreWindow;
                var downState = CoreVirtualKeyStates.Down;
                var virtualKey = args.VirtualKey;
                bool menuKey = (coreWindow.GetKeyState(VirtualKey.Menu) & downState) == downState;
                bool winKey = ((coreWindow.GetKeyState(VirtualKey.LeftWindows) & downState) == downState || (coreWindow.GetKeyState(VirtualKey.RightWindows) & downState) == downState);
                bool controlKey = (coreWindow.GetKeyState(VirtualKey.Control) & downState) == downState;
                bool shiftKey = (coreWindow.GetKeyState(VirtualKey.Shift) & downState) == downState;
                bool noModifiers = !menuKey && !controlKey && !shiftKey && !winKey;
                bool onlyAlt = menuKey && !controlKey && !shiftKey && !winKey;

                //TODO: DeviceGestureService.KeyDown event?

                if (((int)virtualKey == 166 && noModifiers) || (virtualKey == VirtualKey.Left && onlyAlt))
                {
                    // When the previous key or Alt+Left are pressed navigate back
                    args.Handled = true;
                    RaiseCancelableEvent<DeviceGestureEventArgs>(GoBackRequested, this, new DeviceGestureEventArgs());
                }
                else if (virtualKey == VirtualKey.Back && winKey)
                {
                    // When Win+Backspace is pressed navigate back
                    args.Handled = true;
                    RaiseCancelableEvent<DeviceGestureEventArgs>(GoBackRequested, this, new DeviceGestureEventArgs());
                }
                else if (((int)virtualKey == 167 && noModifiers) || (virtualKey == VirtualKey.Right && onlyAlt))
                {
                    // When the next key or Alt+Right are pressed navigate forward
                    args.Handled = true;
                    RaiseCancelableEvent<DeviceGestureEventArgs>(GoForwardRequested, this, new DeviceGestureEventArgs());
                }
            }
        }

        /// <summary>
        /// Invoked on every mouse click, touch screen tap, or equivalent interaction.
        /// Used to detect browser-style next and previous mouse button clicks to navigate between pages.
        /// </summary>
        /// <param name="sender">Instance that triggered the event.</param>
        /// <param name="args">Event data describing the conditions that led to the event.</param>
        protected virtual void OnPointerPressed(CoreWindow sender, PointerEventArgs args)
        {
            var properties = args.CurrentPoint.Properties;

            // Ignore button chords with the left, right, and middle buttons
            if (properties.IsLeftButtonPressed || properties.IsRightButtonPressed || properties.IsMiddleButtonPressed)
                return;

            // If back or foward are pressed (but not both) navigate appropriately
            bool backPressed = properties.IsXButton1Pressed;
            bool forwardPressed = properties.IsXButton2Pressed;

            if (backPressed ^ forwardPressed)
            {
                args.Handled = true;

                if (backPressed)
                    RaiseCancelableEvent<DeviceGestureEventArgs>(GoBackRequested, this, new DeviceGestureEventArgs());

                if (forwardPressed)
                    RaiseCancelableEvent<DeviceGestureEventArgs>(GoForwardRequested, this, new DeviceGestureEventArgs());
            }
        }
    }
}