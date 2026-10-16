using System.Reflection;
using CrossTraining.Application.Features.Authentication;
using CrossTraining.Domain.Entities;
using CrossTraining.Infrastructure.Persistence;
using CrossTraining.Web.Controllers;
using NetArchTest.Rules;

namespace CrossTraining.ArchitectureTests;

/// <summary>
/// Regulile din ADR-0001 si ADR-0006 nu sunt verificate de compilator: nimic nu
/// impiedica un controller sa injecteze direct AppDbContext, sau un port sa reapara
/// in Domain. Testele de aici le impun.
/// </summary>
public class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(AppUser).Assembly;
    private static readonly Assembly Application = typeof(LoginUserHandler).Assembly;
    private static readonly Assembly Infrastructure = typeof(AppDbContext).Assembly;
    private static readonly Assembly Web = typeof(HomeController).Assembly;

    private const string ApplicationNamespace = "CrossTraining.Application";
    private const string InfrastructureNamespace = "CrossTraining.Infrastructure";
    private const string WebNamespace = "CrossTraining.Web";
    private const string PortsNamespace = "CrossTraining.Application.Abstractions";

    [Fact]
    public void Domain_NuDepindeDeNiciunAltStrat()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, WebNamespace)
            .GetResult();

        AssertSuccess(result, "Domain trebuie sa ramana in centru, fara dependinte spre exterior.");
    }

    [Fact]
    public void Domain_NuStieDePersistentaSauDeWeb()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        AssertSuccess(result, "Entitatile de domeniu nu au voie sa cunoasca EF Core sau ASP.NET Core.");
    }

    [Fact]
    public void Domain_NuGazduiestePorturi()
    {
        // ADR-0006: porturile stau in Application.Abstractions. Domain are voie sa aiba
        // contracte proprii modelului - ISoftDeletable, implementat de entitatile insesi -
        // dar niciun contract implementat de infrastructura.
        var ports = Types.InAssembly(DomainAssembly)
            .That().AreInterfaces()
            .GetTypes()
            .Where(t => t.Name.EndsWith("Repository", StringComparison.Ordinal)
                     || t.Name.EndsWith("Service", StringComparison.Ordinal)
                     || t.Name.EndsWith("UnitOfWork", StringComparison.Ordinal))
            .Select(t => t.FullName)
            .ToList();

        ports.ShouldBeEmpty(
            $"Porturile se muta in {PortsNamespace}. Gasite in Domain: {string.Join(", ", ports)}");
    }

    [Fact]
    public void Application_NuDepindeDeInfrastructureSauWeb()
    {
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, WebNamespace)
            .GetResult();

        AssertSuccess(result, "Application defineste porturile; implementarile stau in Infrastructure.");
    }

    [Fact]
    public void Application_NuFoloseresteEfCore()
    {
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        AssertSuccess(result, "Accesul la date trece prin porturi, nu prin EF Core.");
    }

    [Fact]
    public void Application_NuDepindeDeBiblioteciDeUi()
    {
        // X.PagedList este o dependinta de prezentare. A stat in Application pana in
        // etapa 4; testul exista ca sa nu se intoarca.
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOnAny("X.PagedList", "Microsoft.AspNetCore")
            .GetResult();

        AssertSuccess(result, "Application nu are voie sa cunoasca stratul de prezentare.");
    }

    [Fact]
    public void Controllerele_NuAtingDirectInfrastructure()
    {
        // Web refera Infrastructure doar ca sa poata apela AddInfrastructure la pornire.
        // Controllerele trebuie sa lucreze exclusiv prin interfetele din Application.
        var result = Types.InAssembly(Web)
            .That().HaveNameEndingWith("Controller")
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        AssertSuccess(result, "Un controller care atinge Infrastructure ocoleste stratul Application.");
    }

    [Fact]
    public void Controllerele_NuExpunEntitatiDeDomeniu()
    {
        // Entitatile nu au voie sa apara in semnaturile actiunilor: nici la model
        // binding (over-posting), nici catre view-uri (cuplare view <-> schema DB).
        var offenders = Types.InAssembly(Web)
            .That().HaveNameEndingWith("Controller")
            .GetTypes()
            .SelectMany(controller => controller
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .SelectMany(method => method.GetParameters().Select(p => (Controller: controller, method.Name, p.ParameterType))))
            .Where(x => x.ParameterType.Assembly == DomainAssembly)
            .Select(x => $"{x.Controller.Name}.{x.Name}({x.ParameterType.Name})")
            .ToList();

        offenders.ShouldBeEmpty(
            $"Actiunile trebuie sa lege pe modele de formular, nu pe entitati: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Nimic_InAfaraDeInfrastructure_NuFoloseresteDbContextul()
    {
        foreach (var assembly in new[] { DomainAssembly, Application })
        {
            var result = Types.InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOn(typeof(AppDbContext).FullName)
                .GetResult();

            AssertSuccess(result, $"{assembly.GetName().Name} nu are voie sa vada AppDbContext.");
        }
    }

    [Fact]
    public void Repositoryurile_StauInLoculLorSiAuUnPortCorespunzator()
    {
        var placement = Types.InAssembly(Infrastructure)
            .That().HaveNameEndingWith("Repository")
            .And().AreClasses()
            .Should()
            .ResideInNamespace($"{InfrastructureNamespace}.Repositories")
            .GetResult();

        AssertSuccess(placement, "Repository-urile stau in Infrastructure.Repositories.");

        // Un repository fara port nu poate fi injectat prin abstractie, deci ar forta
        // consumatorul sa depinda de Infrastructure.
        var ports = Types.InAssembly(Application)
            .That().ResideInNamespace($"{PortsNamespace}.Persistence")
            .And().AreInterfaces()
            .GetTypes()
            .Select(t => t.Name)
            .ToHashSet(StringComparer.Ordinal);

        var implementations = Types.InAssembly(Infrastructure)
            .That().HaveNameEndingWith("Repository")
            .And().AreClasses()
            .GetTypes();

        foreach (var implementation in implementations)
        {
            ports.ShouldContain(
                $"I{implementation.Name}",
                $"{implementation.Name} nu are port corespunzator in {PortsNamespace}.Persistence.");
        }
    }


    [Fact]
    public void Domain_NuStieDeMediatRSauDeValidare()
    {
        // Cazurile de utilizare sunt un concept al stratului Application. Domain ramane
        // curat si compilabil fara niciun pachet.
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("MediatR", "FluentValidation")
            .GetResult();

        AssertSuccess(result, "Domain nu are voie sa depinda de infrastructura de mesagerie.");
    }

    [Fact]
    public void Handlerele_StauInFeliileDeFunctionalitate()
    {
        var result = Types.InAssembly(Application)
            .That().HaveNameEndingWith("Handler")
            .And().AreClasses()
            .Should()
            .ResideInNamespaceStartingWith($"{ApplicationNamespace}.Features")
            .GetResult();

        AssertSuccess(result, "Un handler se gaseste dupa cazul de utilizare, nu dupa tipul lui.");
    }

    [Fact]
    public void Controllerele_NuAtingDirectPorturileDePersistenta()
    {
        // Controllerele trimit comenzi si interogari. Daca ar injecta un repository, ar
        // ocoli pipeline-ul - deci si validarea, si tranzactia, si logarea.
        var result = Types.InAssembly(Web)
            .That().HaveNameEndingWith("Controller")
            .ShouldNot()
            .HaveDependencyOn($"{PortsNamespace}.Persistence")
            .GetResult();

        AssertSuccess(result, "Accesul la date din controllere trece prin ISender.");
    }

    private static void AssertSuccess(TestResult result, string because)
    {
        var failing = result.FailingTypeNames is null
            ? string.Empty
            : string.Join(", ", result.FailingTypeNames);

        result.IsSuccessful.ShouldBeTrue($"{because} Tipuri care incalca regula: {failing}");
    }
}
