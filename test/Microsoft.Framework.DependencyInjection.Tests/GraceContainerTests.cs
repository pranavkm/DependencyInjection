using Grace.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Grace;
using Microsoft.Framework.DependencyInjection.Tests.Fakes;
using System;

namespace Microsoft.Framework.DependencyInjection.Tests
{
    /// <summary>
    /// Summary description for GraceContainerTests
    /// </summary>
    public class GraceContainerTests : ScopingContainerTestBase
    {
        protected override IServiceProvider CreateContainer()
        {
            return CreateContainer(new FakeFallbackServiceProvider());
        }

        protected override IServiceProvider CreateContainer(IServiceProvider fallbackProvider)
        {
            DependencyInjectionContainer container = new DependencyInjectionContainer { ThrowExceptions = true };

            GraceRegistration.Populate(container, TestServices.DefaultServices(), fallbackProvider);

            return container.Locate<IServiceProvider>();
            //var builder = new ContainerBuilder();

            //AutofacRegistration.Populate(
            //    builder,
            //    TestServices.DefaultServices(),
            //    fallbackProvider);

            //IContainer container = builder.Build();
            //return container.Resolve<IServiceProvider>();
        }
    }
}