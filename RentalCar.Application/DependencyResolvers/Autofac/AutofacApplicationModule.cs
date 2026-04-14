using System.Reflection;
using Autofac;
using Autofac.Extras.DynamicProxy;
using Castle.DynamicProxy;
using RentalCar.Core.Interceptors;
using Module = Autofac.Module;

namespace RentalCar.Application.DependencyResolvers.Autofac;

public class AutofacApplicationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        var applicationAssembly = Assembly.GetExecutingAssembly();
        var infrastructureAssembly = Assembly.Load("RentalCar.Infrastructure");

        builder.RegisterAssemblyTypes(applicationAssembly)
            .AsImplementedInterfaces()
            .EnableInterfaceInterceptors(new ProxyGenerationOptions
            {
                Selector = new AspectInterceptorSelector()
            })
            .InstancePerLifetimeScope();

        builder.RegisterAssemblyTypes(infrastructureAssembly)
            .Where(t => t.Name != "SmtpEmailSender")
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();
    }
}