using Autenticacao.Adapters.Controllers;
using Autenticacao.Application.Commands.Login;
using Autenticacao.Application.Options;
using Autenticacao.Application.Services;
using Autenticacao.Infrastructure.Services;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Autenticacao.Infrastructure;

public static class AutenticacaoModule
{
    public static IServiceCollection AddAutenticacaoModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AdminUserOptions>(
            configuration.GetSection(AdminUserOptions.SectionName));

        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        #region Controllers CA

        services.AddScoped<AutenticacaoController>();

        #endregion

        var applicationAssembly = typeof(LoginCommand).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(applicationAssembly);
            cfg.AddOpenBehavior(typeof(SharedKernel.Application.Behaviors.LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(SharedKernel.Application.Behaviors.ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(SharedKernel.Application.Behaviors.TransactionBehavior<,>));
        });

        services.AddValidatorsFromAssembly(applicationAssembly);

        return services;
    }
}
