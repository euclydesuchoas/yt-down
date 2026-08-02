using FluentAssertions;
using Moq;
using YTDown.Application.DTOs;
using YTDown.Application.Interfaces;
using YTDown.UI.ViewModels;

namespace YTDown.UnitTests.UI.ViewModels;

public class SettingsViewModelTests
{
    private readonly Mock<ISettingsService> _settingsService = new();

    private SettingsViewModel CreateViewModel() => new(_settingsService.Object);

    private void GivenSettings(SettingsDto settings) =>
        _settingsService
            .Setup(service => service.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

    [Fact]
    public async Task LoadCommand_WithNothingChosenYet_ShowsTheDownloadsFolderAndTheBestQuality()
    {
        GivenSettings(SettingsDto.Default);

        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.DestinationDirectory.Should().BeNull();
        viewModel.DestinationDescription.Should().Be("Sua pasta Downloads");
        viewModel.SelectedQuality.Height.Should().BeNull();
    }

    [Fact]
    public async Task LoadCommand_ShowsWhatTheUserHadChosen()
    {
        GivenSettings(new SettingsDto(@"D:\Videos", 720));

        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.DestinationDescription.Should().Be(@"D:\Videos");
        viewModel.SelectedQuality.Height.Should().Be(720);
    }

    /// <summary>
    /// Uma altura gravada que nao esteja entre as oferecidas nao pode deixar a
    /// lista sem selecao nenhuma.
    /// </summary>
    [Fact]
    public async Task LoadCommand_WithAQualityThatIsNotOffered_FallsBackToTheBestAvailable()
    {
        GivenSettings(new SettingsDto(MaximumHeight: 144));

        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.SelectedQuality.Should().Be(viewModel.QualityOptions[0]);
    }

    [Fact]
    public async Task SaveCommand_KeepsWhatWasChosenOnTheScreen()
    {
        GivenSettings(SettingsDto.Default);

        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.DestinationDirectory = @"E:\Musicas";
        viewModel.SelectedQuality = new DefaultQualityOption(480);

        await viewModel.SaveCommand.ExecuteAsync(null);

        _settingsService.Verify(
            service => service.SaveAsync(
                new SettingsDto(@"E:\Musicas", 480),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UseDownloadsFolderCommand_GoesBackToTheDefaultDestination()
    {
        GivenSettings(new SettingsDto(@"D:\Videos", 720));

        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.UseDownloadsFolderCommand.CanExecute(null).Should().BeTrue();

        viewModel.UseDownloadsFolderCommand.Execute(null);

        viewModel.DestinationDirectory.Should().BeNull();
        viewModel.DestinationDescription.Should().Be("Sua pasta Downloads");
    }

    /// <summary>
    /// Sem pasta escolhida nao ha o que desfazer.
    /// </summary>
    [Fact]
    public async Task UseDownloadsFolderCommand_IsDisabledWhenTheDownloadsFolderIsAlreadyTheDestination()
    {
        GivenSettings(SettingsDto.Default);

        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.UseDownloadsFolderCommand.CanExecute(null).Should().BeFalse();
    }
}
