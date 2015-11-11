using System;
using Grace.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Specification;

namespace Grace.Extensions.DependencyInjection
{
    public class GraceContainerTests : DependencyInjectionSpecificationTests
    {
        protected override IServiceProvider CreateServiceProvider(IServiceCollection serviceCollection)
        {
            DependencyInjectionContainer container = new DependencyInjectionContainer
            {
                ThrowExceptions = false
            };

            GraceRegistration.Populate(container, serviceCollection);

            return container.Locate<IServiceProvider>();
        }
    }
}