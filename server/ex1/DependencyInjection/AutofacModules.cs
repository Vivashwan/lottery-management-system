using Autofac;
using ex1.Lottery;
using Lottery.Messaging;

namespace ex1.DependencyInjection;

/// <summary>
/// Registers every repository by convention: any class in ex1.Repository ending in "Repository"
/// is bound to the interfaces it implements, one instance per HTTP request.
/// A new repository needs no extra wiring.
/// </summary>
public sealed class DataAccessModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(ThisAssembly)
            .Where(t => t.Namespace == "ex1.Repository" && t.Name.EndsWith("Repository"))
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();
    }
}

/// <summary>Registers application services by the same convention, plus the raffle components.</summary>
public sealed class ApplicationServicesModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(ThisAssembly)
            .Where(t => t.Namespace == "ex1.services" && t.Name.EndsWith("Service"))
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();

        // Stateless and thread-safe, so one shared instance is enough
        builder.RegisterType<CryptoRandomSource>().As<IRandomSource>().SingleInstance();
        builder.RegisterType<WeightedWinnerPicker>().As<IWinnerPicker>().SingleInstance();
    }
}

/// <summary>Registers the RabbitMQ publisher as a singleton so the app keeps one broker connection.</summary>
public sealed class MessagingModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<RabbitMqPublisher>().As<IMessagePublisher>().SingleInstance();
    }
}
