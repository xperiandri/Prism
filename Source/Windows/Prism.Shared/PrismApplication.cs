﻿using Prism.Logging;
using Prism.Mvvm;
using Prism.Windows.AppModel;
using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Resources;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Prism.Windows
{
    public abstract class PrismApplication : Application
    {
        private bool _isRestoringFromTermination;

        /// <summary>
        /// Initializes the singleton application object. This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        protected PrismApplication()
            : this(new DebugLogger())
        {
        }


        /// <summary>
        /// Initializes the singleton application object. This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        /// <param name="logger">Logger</param>
        protected PrismApplication(ILoggerFacade logger)
        {
            WindowsRuntimeResourceManager.InjectIntoResxGeneratedApplicationResourcesClass(typeof(Properties.Resources));

            if (logger == null)
                throw new InvalidOperationException("Logger Facade is null");

            Logger = logger;

            Logger.Log("Created Logger", Category.Debug, Priority.Low);

            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Gets the shell user interface
        /// </summary>
        /// <value>The shell user interface.</value>
        protected UIElement Shell { get; private set; }

        /// <summary>
        /// Gets or sets the session state service.
        /// </summary>
        /// <value>
        /// The session state service.
        /// </value>
        protected ISessionStateService SessionStateService { get; private set; }

        /// <summary>
        /// Gets or sets the navigation service.
        /// </summary>
        /// <value>
        /// The navigation service.
        /// </value>
        protected INavigationService NavigationService { get; private set; }

        /// <summary>
        /// Gets or sets the device gesture service.
        /// </summary>
        /// <value>
        /// The device gesture service.
        /// </value>
        protected IDeviceGestureService DeviceGestureService { get; private set; }

        /// <summary>
        /// Factory for creating the ExtendedSplashScreen instance.
        /// </summary>
        /// <value>
        /// The Func that creates the ExtendedSplashScreen. It requires a SplashScreen parameter,
        /// and must return a Page instance.
        /// </value>
        protected Func<SplashScreen, Page> ExtendedSplashScreenFactory { get; set; }

        /// <summary>
        /// Gets a value indicating whether the application is suspending.
        /// </summary>
        /// <value>
        /// <c>true</c> if the application is suspending; otherwise, <c>false</c>.
        /// </value>
        public bool IsSuspending { get; private set; }

        /// <summary>
        /// Gets the <see cref="ILoggerFacade"/> for the application.
        /// </summary>
        /// <value>A <see cref="ILoggerFacade"/> instance.</value>
        protected ILoggerFacade Logger { get; set; }

        /// <summary>
        /// Override this method with logic that will be performed after the application is initialized. For example, navigating to the application's home page.
        /// </summary>
        /// <param name="args">The <see cref="LaunchActivatedEventArgs"/> instance containing the event data.</param>
        protected abstract void OnLaunchApplication(LaunchActivatedEventArgs args);

        /// <summary>
        /// Gets the type of the page based on a page token.
        /// </summary>
        /// <param name="pageToken">The page token.</param>
        /// <returns>The type of the page which corresponds to the specified token.</returns>
        protected virtual Type GetPageType(string pageToken)
        {
            var assemblyQualifiedAppType = this.GetType().AssemblyQualifiedName;

            var pageNameWithParameter = assemblyQualifiedAppType.Replace(this.GetType().FullName, this.GetType().Namespace + ".Views.{0}Page");

            var viewFullName = string.Format(CultureInfo.InvariantCulture, pageNameWithParameter, pageToken);
            var viewType = Type.GetType(viewFullName);

            if (viewType == null)
            {
                var resourceLoader = ResourceLoader.GetForCurrentView(Constants.InfrastructureResourceMapId);
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, resourceLoader.GetString("DefaultPageTypeLookupErrorMessage"), pageToken, this.GetType().Namespace + ".Views"),
nameof(pageToken));
            }

            return viewType;
        }

        /// <summary>
        /// Used for setting up the list of known types for the SessionStateService, using the RegisterKnownType method.
        /// </summary>
        protected virtual void OnRegisterKnownTypesForSerialization() { }

        /// <summary>
        /// Override this method with the initialization logic of your application. Here you can initialize services, repositories, and so on.
        /// </summary>
        /// <param name="args">The <see cref="IActivatedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnInitialize(IActivatedEventArgs args) { }

        /// <summary>
        /// Resolves the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A concrete instance of the specified type.</returns>
        protected virtual object Resolve(Type type) => Activator.CreateInstance(type);

        /// <summary>
        /// Gets a cache size of root frame.
        /// </summary>
        protected virtual int RootFrameCacheSize => 1;

        /// <summary>
        /// Invoked when the application is launched normally by the end user. Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            if (Window.Current.Content == null)
            {
                Frame rootFrame = InitializeFrame(args);

                Shell = CreateShell(rootFrame);

                Window.Current.Content = Shell ?? rootFrame;
            }

            // If the app is launched via the app's primary tile, the args.TileId property
            // will have the same value as the AppUserModelId, which is set in the Package.appxmanifest.
            // See http://go.microsoft.com/fwlink/?LinkID=288842
            string tileId = AppManifestHelper.GetApplicationId();

            if (Window.Current.Content != null && (!_isRestoringFromTermination || args?.TileId != tileId))
            {
                OnLaunchApplication(args);
            }

            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Creates the root frame.
        /// </summary>
        /// <returns>The initialized root frame.</returns>
        private Frame CreateRootFrame() => OnCreateRootFrame() ?? new Frame();

        /// <summary>
        /// Creates the root frame. Use this to inject your own Frame implementation.
        /// </summary>
        /// <returns>The initialized root frame.</returns>
        protected virtual Frame OnCreateRootFrame() => null;

        /// <summary>
        /// Creates the session state service.
        /// </summary>
        /// <returns>The initialized session state service.</returns>
        private ISessionStateService CreateSessionStateService() => OnCreateSessionStateService() ?? new SessionStateService();

        /// <summary>
        /// Creates the session state service. Use this to inject your own ISessionStateService implementation.
        /// </summary>
        /// <returns>The initialized session state service.</returns>
        protected virtual ISessionStateService OnCreateSessionStateService() => null;

        /// <summary>
        /// Initializes the Frame and its content.
        /// </summary>
        /// <param name="args">The <see cref="IActivatedEventArgs"/> instance containing the event data.</param>
        /// <returns>A task of a Frame that holds the app content.</returns>
        protected Frame InitializeFrame(IActivatedEventArgs args)
        {
            // Create a Frame to act as the navigation context and navigate to the first page
            var rootFrame = new Frame() { CacheSize = RootFrameCacheSize };

            if (ExtendedSplashScreenFactory != null)
            {
                Page extendedSplashScreen = this.ExtendedSplashScreenFactory.Invoke(args.SplashScreen);
                rootFrame.Content = extendedSplashScreen;
            }

            rootFrame.Navigated += OnNavigated;

            var frameFacade = new FrameFacadeAdapter(rootFrame);

            //Initialize PrismApplication common services
            SessionStateService = CreateSessionStateService();

            //Configure VisualStateAwarePage with the ability to get the session state for its frame
            VisualStateAwarePage.GetSessionStateForFrame =
                frame => SessionStateService.GetSessionStateForFrame(frameFacade);

            //Associate the frame with a key
            SessionStateService.RegisterFrame(frameFacade, "AppFrame");

            NavigationService = CreateNavigationService(frameFacade, SessionStateService);

            DeviceGestureService = CreateDeviceGestureService();
            DeviceGestureService.GoBackRequested += OnGoBackRequested;
            DeviceGestureService.GoForwardRequested += OnGoForwardRequested;

#if WINDOWS_APP
            global::Windows.UI.ApplicationSettings.SettingsPane.GetForCurrentView().CommandsRequested += OnCommandsRequested;
#endif

            // Set a factory for the ViewModelLocator to use the default resolution mechanism to construct view models
            ViewModelLocationProvider.SetDefaultViewModelFactory(Resolve);

            OnRegisterKnownTypesForSerialization();
            if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
            {
                SessionStateService.RestoreSessionStateAsync().Wait();
            }

            OnInitialize(args);

            if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
            {
                // Restore the saved session state and navigate to the last page visited
                try
                {
                    SessionStateService.RestoreFrameState();
                    NavigationService.RestoreSavedNavigation();
                    _isRestoringFromTermination = true;
                }
                catch (SessionStateServiceException)
                {
                    Logger.Log("Unable to restore session state.", Category.Exception, Priority.None);
                    // Something went wrong restoring state.
                    // Assume there is no state and continue
                }
            }

            return rootFrame;
        }

        /// <summary>
        /// Handling the forward navigation request from the <see cref="IDeviceGestureService"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnGoForwardRequested(object sender, DeviceGestureEventArgs e)
        {
            if (NavigationService.CanGoForward())
            {
                NavigationService.GoForward();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handling the back navigation request from the <see cref="IDeviceGestureService"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnGoBackRequested(object sender, DeviceGestureEventArgs e)
        {
            if (NavigationService.CanGoBack())
            {
                NavigationService.GoBack();
                e.Handled = true;
            }
            else if (DeviceGestureService.IsHardwareBackButtonPresent && e.IsHardwareButton)
            {
                // Looks like default behavior must be to do nothing
                //Exit();
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnNavigated(object sender, NavigationEventArgs e)
        {
#if WINDOWS_UWP
            if (DeviceGestureService.UseTitleBarBackButton)
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                    NavigationService.CanGoBack() ? AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;
#endif
        }

        /// <summary>
        /// Creates the device gesture service. Use this to inject your own IDeviceGestureService implementation.
        /// </summary>
        /// <returns>The initialized device gesture service.</returns>
        protected virtual IDeviceGestureService OnCreateDeviceGestureService() => null;

        /// <summary>
        /// Creates the device gesture service.
        /// </summary>
        /// <returns>The initialized device gesture service.</returns>
        private IDeviceGestureService CreateDeviceGestureService()
        {
            var deviceGestureService = OnCreateDeviceGestureService() ?? new DeviceGestureService {UseTitleBarBackButton = true};
            return deviceGestureService;
        }

        /// <summary>
        /// Creates the navigation service. Use this to inject your own INavigationService implementation.
        /// </summary>
        /// <param name="rootFrame">The root frame.</param>
        /// <returns>The initialized navigation service.</returns>
        protected virtual INavigationService OnCreateNavigationService(IFrameFacade rootFrame) => null;

        /// <summary>
        /// Creates the navigation service.
        /// </summary>
        /// <param name="rootFrame">The root frame.</param>
        /// <param name="sessionStateService">The session state service.</param>
        /// <returns>The initialized navigation service.</returns>
        private INavigationService CreateNavigationService(IFrameFacade rootFrame, ISessionStateService sessionStateService)
        {
            var navigationService = OnCreateNavigationService(rootFrame) ?? new FrameNavigationService(rootFrame, GetPageType, sessionStateService);
            return navigationService;
        }

        /// <summary>
        /// Creates the shell of the app.
        /// </summary>
        /// <param name="rootFrame"></param>
        /// <returns>The shell of the app.</returns>
        protected virtual UIElement CreateShell(Frame rootFrame) => rootFrame;

        /// <summary>
        /// Invoked when application execution is being suspended. Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            IsSuspending = true;
            try
            {
                var deferral = e.SuspendingOperation.GetDeferral();

                //Custom calls before suspending.
                await OnSuspendingApplicationAsync();

                //Bootstrap inform navigation service that app is suspending.
                NavigationService.Suspending();

                // Save application state
                await SessionStateService.SaveAsync();

                deferral.Complete();
            }
            finally
            {
                IsSuspending = false;
            }
        }
#if WINDOWS_APP
        /// <summary>
        /// Gets the Settings charm action items.
        /// </summary>
        /// <returns>The list of Setting charm action items that will populate the Settings pane.</returns>
        protected abstract IEnumerable<global::Windows.UI.ApplicationSettings.SettingsCommand> GetSettingsCommands();

        /// <summary>
        /// Called when the Settings charm is invoked, this handler populates the Settings charm with the charm items returned by the GetSettingsCommands function.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="SettingsPaneCommandsRequestedEventArgs"/> instance containing the event data.</param>
        private void OnCommandsRequested(global::Windows.UI.ApplicationSettings.SettingsPane sender, global::Windows.UI.ApplicationSettings.SettingsPaneCommandsRequestedEventArgs args)
        {
            if (args == null || args.Request == null || args.Request.ApplicationCommands == null)
            {
                return;
            }

            var applicationCommands = args.Request.ApplicationCommands;
            var settingsCommands = GetSettingsCommands();

            foreach (var settingsCommand in settingsCommands)
            {
                applicationCommands.Add(settingsCommand);
            }
        }
#endif

        /// <summary>
        /// Invoked when the application is suspending, but before the general suspension calls.
        /// </summary>
        /// <returns>Task to complete.</returns>
        protected virtual Task OnSuspendingApplicationAsync() => Task.FromResult<object>(null);
    }
}
