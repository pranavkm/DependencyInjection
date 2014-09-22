// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Grace.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;


namespace Microsoft.Framework.DependencyInjection.Grace
{
    public static class GraceRegistration
    {
        public static void Populate(
                IExportLocator exportLocator,
                IEnumerable<IServiceDescriptor> descriptors)
        {
            Populate(exportLocator, descriptors, fallbackServiceProvider: null);
        }

        public static void Populate(IExportLocator exportLocator,
                                    IEnumerable<IServiceDescriptor> descriptors,
                                    IServiceProvider fallbackServiceProvider)
        {
            if(fallbackServiceProvider != null)
            {
                exportLocator.AddMissingExportStrategyProvider(new ChainedMissingExportStrategyProvider(fallbackServiceProvider));
            }

            exportLocator.Configure(c =>
            {
                c.Export<GraceServiceProvider>().As<IServiceProvider>();
                c.Export<GraceServiceScopeFactory>().As<IServiceScopeFactory>();

                Register(c, descriptors);
            });    
        }

        private static void Register(IExportRegistrationBlock c, IEnumerable<IServiceDescriptor> descriptors)
        {
            foreach(var descriptor in descriptors)
            {
                if (descriptor.ImplementationType != null)
                {
                    c.Export(descriptor.ImplementationType).
                      As(descriptor.ServiceType).
                      ConfigureLifecycle(descriptor.Lifecycle);
                }
                else
                {
                    c.ExportInstance(descriptor.ImplementationInstance).
                      As(descriptor.ServiceType).
                      ConfigureLifecycle(descriptor.Lifecycle);
                }
            }
        }

        private static IFluentExportStrategyConfiguration ConfigureLifecycle(this IFluentExportStrategyConfiguration configuration, LifecycleKind lifecycleKind)
        {
            switch (lifecycleKind)
            {
                case LifecycleKind.Scoped:
                    return configuration.Lifestyle.SingletonPerScope();
                    
                case LifecycleKind.Singleton:
                    return configuration.Lifestyle.Singleton();
            }

            return configuration;
        }

        private static IFluentExportInstanceConfiguration<T> ConfigureLifecycle<T>(this IFluentExportInstanceConfiguration<T> configuration, LifecycleKind lifecycleKind)
        {
            switch (lifecycleKind)
            {
                case LifecycleKind.Scoped:
                    return configuration.Lifestyle.SingletonPerScope();

                case LifecycleKind.Singleton:
                    return configuration.Lifestyle.Singleton();
            }

            return configuration;
        }


        //public static void Populate(
        //        ContainerBuilder builder,
        //        IEnumerable<IServiceDescriptor> descriptors,
        //        IServiceProvider fallbackServiceProvider)
        //{
        //    if (fallbackServiceProvider != null)
        //    {
        //        builder.RegisterSource(new ChainedRegistrationSource(fallbackServiceProvider));
        //    }

        //    builder.RegisterType<AutofacServiceProvider>().As<IServiceProvider>();
        //    builder.RegisterType<AutofacServiceScopeFactory>().As<IServiceScopeFactory>();

        //    Register(builder, descriptors);
        //}

        //private static void Register(
        //        ContainerBuilder builder,
        //        IEnumerable<IServiceDescriptor> descriptors)
        //{
        //    foreach (var descriptor in descriptors)
        //    {
        //        if (descriptor.ImplementationType != null)
        //        {
        //            // Test if the an open generic type is being registered
        //            var serviceTypeInfo = descriptor.ServiceType.GetTypeInfo();
        //            if (serviceTypeInfo.IsGenericTypeDefinition)
        //            {
        //                builder
        //                    .RegisterGeneric(descriptor.ImplementationType)
        //                    .As(descriptor.ServiceType)
        //                    .ConfigureLifecycle(descriptor.Lifecycle);
        //            }
        //            else
        //            {
        //                builder
        //                    .RegisterType(descriptor.ImplementationType)
        //                    .As(descriptor.ServiceType)
        //                    .ConfigureLifecycle(descriptor.Lifecycle);
        //            }
        //        }
        //        else
        //        {
        //            builder
        //                .RegisterInstance(descriptor.ImplementationInstance)
        //                .As(descriptor.ServiceType)
        //                .ConfigureLifecycle(descriptor.Lifecycle);
        //        }
        //    }
        //}

        //private static IRegistrationBuilder<object, T, U> ConfigureLifecycle<T, U>(
        //        this IRegistrationBuilder<object, T, U> registrationBuilder,
        //        LifecycleKind lifecycleKind)
        //{
        //    switch (lifecycleKind)
        //    {
        //        case LifecycleKind.Singleton:
        //            registrationBuilder.SingleInstance();
        //            break;
        //        case LifecycleKind.Scoped:
        //            registrationBuilder.InstancePerLifetimeScope();
        //            break;
        //        case LifecycleKind.Transient:
        //            registrationBuilder.InstancePerDependency();
        //            break;
        //    }

        //    return registrationBuilder;
        //}

        private class GraceServiceProvider : IServiceProvider
        {
            private readonly IInjectionScope _injectionScope;

            public GraceServiceProvider(IInjectionScope injectionScope)
            {
                _injectionScope = injectionScope;
            }

            public object GetService(Type serviceType)
            {
                return _injectionScope.Locate(serviceType);
            }
        }

        private class GraceServiceScopeFactory : IServiceScopeFactory
        {
            private readonly IInjectionScope _injectionScope;

            public GraceServiceScopeFactory(IInjectionScope injectionScope)
            {
                _injectionScope = injectionScope;
            }

            public IServiceScope CreateScope()
            {
                return new GraceServiceScope(_injectionScope.CreateChildScope());
            }
        }

        private class GraceServiceScope : IServiceScope
        {
            private IInjectionScope _injectionScope;
            private readonly IServiceProvider _serviceProvider;
            private bool disposedValue = false; // To detect redundant calls

            public GraceServiceScope(IInjectionScope injectionScope)
            {
                _injectionScope = injectionScope;
                _serviceProvider = _injectionScope.Locate<IServiceProvider>();
            }

            public IServiceProvider ServiceProvider
            {
                get { return _serviceProvider; }
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        _injectionScope.Dispose();
                    }

                    disposedValue = true;
                }
            }

            // This code added to correctly implement the disposable pattern.
            public void Dispose()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(true);
                // TODO: tell GC not to call its finalizer when the above finalizer is overridden.
                // GC.SuppressFinalize(this);
            }
        }
    }
}
