using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using YTDown.Application.Interfaces;
using YTDown.Domain.ValueObjects;
using YTDown.Infrastructure.Processes;

namespace YTDown.ArchitectureTests;

/// <summary>
/// Fixa a direcao das dependencias entre as camadas.
/// </summary>
/// <remarks>
/// Um using acidental basta para inverter uma dependencia sem quebrar o build.
/// Estes testes transformam isso em falha visivel.
/// </remarks>
public class LayerDependencyTests
{
    private const string DomainNamespace = "YTDown.Domain";
    private const string ApplicationNamespace = "YTDown.Application";
    private const string InfrastructureNamespace = "YTDown.Infrastructure";
    private const string UiNamespace = "YTDown.UI";

    private const string ProcessType = "System.Diagnostics.Process";
    private const string WpfNamespace = "System.Windows";

    private static readonly Assembly DomainAssembly = typeof(VideoUrl).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(IVideoInfoService).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(ProcessRunner).Assembly;
    private static readonly Assembly UiAssembly = typeof(UI.App).Assembly;

    [Fact]
    public void Domain_DependsOnNoOtherLayer()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, UiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(because: Describe(result));
    }

    [Fact]
    public void Domain_KnowsNothingAboutTheOutsideWorld()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ProcessType, WpfNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(because: Describe(result));
    }

    [Fact]
    public void Application_DependsOnNeitherInfrastructureNorUi()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, UiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(because: Describe(result));
    }

    [Fact]
    public void Application_UsesNoWpfTypeAndStartsNoProcess()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(WpfNamespace, ProcessType)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(because: Describe(result));
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnUi()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(UiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(because: Describe(result));
    }

    /// <summary>
    /// A apresentacao nunca executa ferramenta externa: toda execucao passa pela
    /// Application e termina na Infrastructure.
    /// </summary>
    [Fact]
    public void Ui_StartsNoProcess()
    {
        var result = Types.InAssembly(UiAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ProcessType)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(because: Describe(result));
    }

    /// <summary>
    /// Controle positivo das regras acima.
    /// </summary>
    /// <remarks>
    /// Uma regra que nunca acusa nada passa mesmo quando a deteccao esta quebrada.
    /// Esta verificacao afirma uma dependencia que sabidamente existe: se ela
    /// parar de ser detectada, os testes de proibicao viraram decoracao.
    /// </remarks>
    [Fact]
    public void DependencyDetection_ActuallySeesAnExistingDependency()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .HaveNameStartingWith("YtDlpMetadataProvider")
            .Should()
            .HaveDependencyOnAny(ApplicationNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(because: Describe(result));
    }

    /// <summary>
    /// Monta a explicacao da falha, dizendo qual tipo violou a regra e por que.
    /// </summary>
    private static string Describe(TestResult result) =>
        result.FailingTypes is null or []
            ? "nenhum tipo violou a regra"
            : "estes tipos violam a regra: " + string.Join(
                "; ",
                result.FailingTypes.Select(type => $"{type.FullName} ({type.Explanation})"));
}
