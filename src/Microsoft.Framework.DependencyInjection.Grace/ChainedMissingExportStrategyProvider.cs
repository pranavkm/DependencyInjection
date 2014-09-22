// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Grace;
using Grace.DependencyInjection;
using Grace.DependencyInjection.Impl;
using System.Collections;
using Grace.DependencyInjection.Lifestyle;

namespace Microsoft.Framework.DependencyInjection.Grace
{
    public class ChainedMissingExportStrategyProvider : IMissingExportStrategyProvider
    {
        private readonly IServiceProvider _fallbackServiceProvider;

        public ChainedMissingExportStrategyProvider(IServiceProvider fallbackServiceProvider)
        {
            _fallbackServiceProvider = fallbackServiceProvider;
        }

        //public bool IsAdapterForIndividualComponents
        //{
        //    get { return false; }
        //}

        //public IEnumerable<IComponentRegistration> RegistrationsFor(
        //        Service service,
        //        Func<Service, IEnumerable<IComponentRegistration>> registrationAcessor)
        //{
        //    var serviceWithType = service as IServiceWithType;
        //    if (serviceWithType == null)
        //    {
        //        yield break;
        //    }

        //    // Only introduce services that are not already registered
        //    if (registrationAcessor(service).Any())
        //    {
        //        yield break;
        //    }

        //    var serviceType = serviceWithType.ServiceType;
        //    if (serviceType == typeof(FallbackScope))
        //    {
        //        // This is where we rescope the _fallbackServiceProvider for use in inner scopes
        //        // When we actually resolve fallback services, we first access the scoped fallback
        //        // service provider by resolving FallbackScope and using its ServiceProvider property.
        //        yield return RegistrationBuilder.ForDelegate(serviceType, (context, p) =>
        //        {
        //            var lifetime = context.Resolve<ILifetimeScope>() as ISharingLifetimeScope;

        //            if (lifetime != null)
        //            {
        //                var parentLifetime = lifetime.ParentLifetimeScope;

        //                FallbackScope parentFallback;
        //                if (parentLifetime != null &&
        //                    parentLifetime.TryResolve<FallbackScope>(out parentFallback))
        //                {
        //                    var scopeFactory = parentFallback.ServiceProvider
        //                        .GetServiceOrDefault<IServiceScopeFactory>();

        //                    if (scopeFactory != null)
        //                    {
        //                        return new FallbackScope(scopeFactory.CreateScope());
        //                    }
        //                }
        //            }

        //            return new FallbackScope(_fallbackServiceProvider);
        //        })
        //        .InstancePerLifetimeScope()
        //        .CreateRegistration();
        //    }
        //    else if (_fallbackServiceProvider.GetServiceOrNull(serviceType) != null)
        //    {
        //        yield return RegistrationBuilder.ForDelegate(serviceType, (context, p) =>
        //        {
        //            var fallbackScope = context.Resolve<FallbackScope>();
        //            return fallbackScope.ServiceProvider.GetService(serviceType);
        //        })
        //        .PreserveExistingDefaults()
        //        .CreateRegistration();
        //    }
        //}

        //private class FallbackScope : IDisposable
        //{
        //    private readonly IDisposable _scopeDisposer;

        //    public FallbackScope(IServiceProvider fallbackServiceProvider)
        //        : this(fallbackServiceProvider, scopeDisposer: null)
        //    {
        //    }
        //    public FallbackScope(IServiceScope fallbackScope)
        //        : this(fallbackScope.ServiceProvider, fallbackScope)
        //    {
        //    }

        //    private FallbackScope(IServiceProvider fallbackServiceProvider, IDisposable scopeDisposer)
        //    {
        //        ServiceProvider = fallbackServiceProvider;
        //        _scopeDisposer = scopeDisposer;
        //    }

        //    public IServiceProvider ServiceProvider { get; private set; }

        //    public void Dispose()
        //    {
        //        if (_scopeDisposer != null)
        //        {
        //            _scopeDisposer.Dispose();
        //        }
        //    }
        //}
        //public IEnumerable<IExportStrategy> ProvideExports(IInjectionContext requestContext, string exportName, Type exportType, ExportStrategyFilter consider, object locateKey)
        //{
        //    throw new NotImplementedException();
        //}
        public IEnumerable<IExportStrategy> ProvideExports(IInjectionContext requestContext, string exportName, Type exportType, ExportStrategyFilter consider, object locateKey)
        {
            if(exportType == null)
            {
                yield break;
            }

            if(exportType == typeof(FallbackScope))
            {
                yield return new FallBackExportStrategy(_fallbackServiceProvider);
            }
            else if(!ExportAlreadyExist(requestContext.RequestingScope, exportType))
            {
                var returnInstance = _fallbackServiceProvider.GetServiceOrNull(exportType);

                if(returnInstance == null)
                {
                    yield break;
                }

                IEnumerable enumerableInstance = returnInstance as IEnumerable;

                if(enumerableInstance != null && !enumerableInstance.GetEnumerator().MoveNext())
                {
                    yield break;
                }

                yield return new MissingExportStrategy(exportType);
            }

            yield break;
        }

        private bool ExportAlreadyExist(IInjectionScope injectionScope, Type exportType)
        {
            if (exportType.IsConstructedGenericType && 
                exportType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return CheckScopeForExport(injectionScope, exportType.GetGenericArguments().First());
            }

            return false;
        }

        private bool CheckScopeForExport(IInjectionScope injectionScope, Type exportType)
        {
            if (injectionScope.GetStrategy(exportType) != null)
            {
                return true;
            }
            else if(injectionScope.ParentScope != null)
            {
                return CheckScopeForExport(injectionScope.ParentScope, exportType);
            }

            return false;
        }
    }

    internal class MissingExportStrategy : ConfigurableExportStrategy
    {
        public MissingExportStrategy(Type type) : base(type)
        {

        }

        public override object Activate(IInjectionScope exportInjectionScope, IInjectionContext context, ExportStrategyFilter consider, object locateKey)
        {
            var fallback = context.RequestingScope.Locate<FallbackScope>(context);

            return fallback.ServiceProvider.GetService(ActivationType);
        }
    }

    internal class FallBackExportStrategy : ConfigurableExportStrategy
    {
        private IServiceProvider _provider;

        public FallBackExportStrategy(IServiceProvider provider) : base(typeof(FallbackScope))
        {
            _provider = provider;
            SetLifestyleContainer(new SingletonPerScopeLifestyle());
        }

        public override object Activate(IInjectionScope exportInjectionScope, IInjectionContext context, ExportStrategyFilter consider, object locateKey)
        {
            return Lifestyle.Locate(CreateMethod, exportInjectionScope, context, this);            
        }

        private object CreateMethod(IInjectionScope injectionScope, IInjectionContext context)
        {
            var parentScope = context.RequestingScope.ParentScope;

            if (parentScope != null)
            {
                FallbackScope parentFallbackScope = parentScope.Locate<FallbackScope>();

                if (parentFallbackScope != null)
                {
                    IServiceScopeFactory scopeFactory =
                        parentFallbackScope.ServiceProvider.GetServiceOrDefault<IServiceScopeFactory>();

                    if (scopeFactory != null)
                    {
                        var returnScope = new FallbackScope(scopeFactory.CreateScope());

                        return returnScope;
                    }
                }
            }

            return new FallbackScope(_provider);
        }
    }

    internal class FallbackScope : IDisposable
    {
        private readonly IDisposable _scopeDisposer;

        public FallbackScope(IServiceProvider fallbackServiceProvider)
            : this(fallbackServiceProvider, scopeDisposer: null)
        {
        }

        public FallbackScope(IServiceScope fallbackScope)
            : this(fallbackScope.ServiceProvider, fallbackScope)
        {
        }

        private FallbackScope(IServiceProvider fallbackServiceProvider, IDisposable scopeDisposer)
        {
            ServiceProvider = fallbackServiceProvider;
            _scopeDisposer = scopeDisposer;
        }

        public IServiceProvider ServiceProvider { get; private set; }

        public void Dispose()
        {
            if (_scopeDisposer != null)
            {
                _scopeDisposer.Dispose();
            }
        }
    }
}
