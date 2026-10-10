using Autofac;
using Autofac.Extensions.DependencyInjection;
using ex1.Controllers;
using ex1.Data;
using ex1.DependencyInjection;
using ex1.Interfaces.Repositorys;
using ex1.Interfaces.Services;
using ex1.Lottery;
using ex1.services;
using FluentAssertions;
using Lottery.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TestProject1.DependencyInjection;

/// <summary>
/// Builds the real Autofac container and resolves every controller, so a missing or
/// mis-scoped registration fails here instead of at runtime on the first request.
/// </summary>
public class AutofacCompositionTests
{
    private static IContainer BuildContainer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "test-secret-key-that-is-long-enough-123456",
            })
            .Build());
        // Resolving the context never opens a connection, so no database is needed
        services.AddDbContext<StoreContext>(o => o.UseSqlServer("Server=unused;Database=unused;"));
        services.Configure<RabbitMqOptions>(_ => { });

        var builder = new ContainerBuilder();
        builder.Populate(services);
        builder.RegisterModule<DataAccessModule>();
        builder.RegisterModule<ApplicationServicesModule>();
        builder.RegisterModule<MessagingModule>();
        builder.RegisterAssemblyTypes(typeof(LotteryController).Assembly)
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .AsSelf();

        return builder.Build();
    }

    public static TheoryData<Type> Controllers()
    {
        var data = new TheoryData<Type>();
        foreach (var type in typeof(LotteryController).Assembly.GetTypes()
                     .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract))
            data.Add(type);
        return data;
    }

    [Theory]
    [MemberData(nameof(Controllers))]
    public void Every_controller_can_be_resolved(Type controllerType)
    {
        using var container = BuildContainer();
        using var scope = container.BeginLifetimeScope();

        scope.Resolve(controllerType).Should().NotBeNull();
    }

    [Fact]
    public void Repositories_and_services_are_registered_by_convention()
    {
        using var container = BuildContainer();

        container.IsRegistered<ILotteryRepository>().Should().BeTrue();
        container.IsRegistered<IUserRepository>().Should().BeTrue();
        container.IsRegistered<ILotteryService>().Should().BeTrue();
        container.IsRegistered<ITokenService>().Should().BeTrue();
    }

    [Fact]
    public void Repositories_are_scoped_per_request()
    {
        using var container = BuildContainer();
        using var request1 = container.BeginLifetimeScope();
        using var request2 = container.BeginLifetimeScope();

        request1.Resolve<ILotteryRepository>().Should().BeSameAs(request1.Resolve<ILotteryRepository>());
        request1.Resolve<ILotteryRepository>().Should().NotBeSameAs(request2.Resolve<ILotteryRepository>());
    }

    [Fact]
    public void Publisher_and_random_source_are_shared_singletons()
    {
        using var container = BuildContainer();
        using var request1 = container.BeginLifetimeScope();
        using var request2 = container.BeginLifetimeScope();

        request1.Resolve<IMessagePublisher>().Should().BeSameAs(request2.Resolve<IMessagePublisher>());
        request1.Resolve<IRandomSource>().Should().BeOfType<CryptoRandomSource>();
    }
}
