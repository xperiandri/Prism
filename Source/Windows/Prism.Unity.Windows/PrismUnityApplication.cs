﻿using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;
using Prism.Events;
using Prism.Logging;
using Prism.Mvvm;
using Prism.Windows;
using Prism.Windows.AppModel;
using Prism.Windows.Navigation;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;

namespace Prism.Unity.Windows
{
    /// <summary>
    /// Provides the base class for the Windows Store Application object which
    /// includes the automatic creation and wiring of the Unity container and
    /// the bootstrapping process for Prism services in the container.
    /// </summary>
    public abstract class PrismUnityApplication : PrismApplication, IDisposable
    {
        #region Constructor

        protected PrismUnityApplication() : this(new DebugLogger(), new UnityContainer())
        {
        }

        protected PrismUnityApplication(ILoggerFacade logger, IUnityContainer container) : base(logger)
        {
            if (container == null)
                throw new InvalidOperationException("Unity container is null");

            Container = container;
        }

        #endregion Constructor

        #region Properties

        /// <summary>
        /// Allow strongly typed access to the Application as a global
        /// </summary>
        public static new PrismUnityApplication Current => (PrismUnityApplication)Application.Current;

        /// <summary>
        /// Get the IoC Unity Container
        /// </summary>
        public IUnityContainer Container { get; private set; }

        #endregion Properties

        #region Overrides

        /// <summary>
        /// Implements and seals the OnInitialize method to configure the container.
        /// </summary>
        /// <param name="args">The <see cref="IActivatedEventArgs"/> instance containing the event data.</param>
        protected override void OnInitialize(IActivatedEventArgs args)
        {
            ConfigureContainer();
            ConfigureViewModelLocator();
        }

        /// <summary>
        /// Implements and seals the Resolves method to be handled by the Unity Container.
        /// Use the container to resolve types (e.g. ViewModels and Flyouts)
        /// so their dependencies get injected
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A concrete instance of the specified type.</returns>
        protected override sealed object Resolve(Type type) => Container.Resolve(type);

        #endregion Overrides

        #region Protected Methods

        /// <summary>
        /// Configures the <see cref="ViewModelLocator"/> used by Prism.
        /// </summary>
        protected virtual void ConfigureViewModelLocator()
        {
            ViewModelLocationProvider.SetDefaultViewModelFactory(Resolve);
        }

        protected virtual void ConfigureContainer()
        {
            // Register the unity container with itself so that it can be dependency injected
            // for programmatic registration and resolving of types
            Container.RegisterInstance(Container);

            // Set up the global locator service for any Prism framework code that needs DI
            // without being coupled to Unity
            Logger.Log("Setting up ServiceLocator", Category.Debug, Priority.Low);
            var serviceLocator = new UnityServiceLocator(Container);
            ServiceLocator.SetLocatorProvider(() => serviceLocator);
            Container.RegisterInstance<IServiceLocator>(serviceLocator);

            Logger.Log("Adding UnityExtensions to container", Category.Debug, Priority.Low);
            Container.AddNewExtension<PrismUnityExtension>();

            Logger.Log("Registering Prism services with container", Category.Debug, Priority.Low);
            Container.RegisterInstance<ILoggerFacade>(Logger);
            Container.RegisterInstance<ISessionStateService>(SessionStateService);
            Container.RegisterInstance<INavigationService>(NavigationService);
            Container.RegisterInstance<IDeviceGestureService>(DeviceGestureService);
            RegisterTypeIfMissing(typeof(IEventAggregator), typeof(EventAggregator), true);
        }

        /// <summary>
        /// Registers a type in the container only if that type was not already registered.
        /// </summary>
        /// <param name="fromType">The interface type to register.</param>
        /// <param name="toType">The type implementing the interface.</param>
        /// <param name="registerAsSingleton">Registers the type as a singleton.</param>
        protected void RegisterTypeIfMissing(Type fromType, Type toType, bool registerAsSingleton)
        {
            if (fromType == null)
            {
                throw new ArgumentNullException(nameof(fromType));
            }
            if (toType == null)
            {
                throw new ArgumentNullException(nameof(toType));
            }
            if (Container.IsTypeRegistered(fromType))
            {
                Logger.Log(
                    string.Format(CultureInfo.CurrentCulture,
                                  "Type {0} already registered with container",
                                  fromType.Name), Category.Debug, Priority.Low);
            }
            else
            {
                if (registerAsSingleton)
                {
                    Container.RegisterType(fromType, toType, new ContainerControlledLifetimeManager());
                }
                else
                {
                    Container.RegisterType(fromType, toType);
                }
            }
        }

        #endregion Protected Methods

        #region IDisposable

        public void Dispose()
        {
            if (Container != null)
            {
                Container.Dispose();
                Container = null;
            }
        }

        #endregion IDisposable
    }
}