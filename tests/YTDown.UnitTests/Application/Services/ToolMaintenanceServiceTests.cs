using FluentAssertions;
using Moq;
using YTDown.Application.Common;
using YTDown.Application.Interfaces;
using YTDown.Application.Services;

namespace YTDown.UnitTests.Application.Services;

public class ToolMaintenanceServiceTests
{
    private readonly Mock<IToolInstaller> _installer = new();
    private readonly Mock<IToolUpdater> _updater = new();

    private readonly List<ToolMaintenanceStatus> _reported = [];

    private ToolMaintenanceService CreateService() => new(_installer.Object, _updater.Object);

    private Task PrepareAsync() =>
        CreateService().PrepareAsync(new SynchronousProgress(_reported.Add), CancellationToken.None);

    private void GivenInstallationReturns(Result<bool> result) =>
        _installer
            .Setup(installer => installer.EnsureInstalledAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

    private void GivenUpdateReturns(Result<string> result) =>
        _updater
            .Setup(updater => updater.UpdateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

    [Fact]
    public async Task PrepareAsync_WhenEverythingWorks_EndsReady()
    {
        GivenInstallationReturns(Result<bool>.Success(true));
        GivenUpdateReturns(Result<string>.Success("2026.07.04"));

        await PrepareAsync();

        _reported.Should().Equal(
            ToolMaintenanceStatus.Installing,
            ToolMaintenanceStatus.CheckingForUpdate,
            ToolMaintenanceStatus.Ready);
    }

    /// <summary>
    /// Ficar sem atualizar quase sempre e falta de internet, e nao impede o uso.
    /// </summary>
    [Fact]
    public async Task PrepareAsync_WhenTheUpdateFails_SaysSoWithoutTreatingItAsBroken()
    {
        GivenInstallationReturns(Result<bool>.Success(false));
        GivenUpdateReturns(Result<string>.Failure(ErrorCode.NetworkError));

        await PrepareAsync();

        _reported.Should().EndWith(ToolMaintenanceStatus.UpdateUnavailable);
    }

    /// <summary>
    /// Sem a copia gravavel nao ha o que atualizar, mas o aplicativo recorre a
    /// copia que acompanha a instalacao e continua utilizavel.
    /// </summary>
    [Fact]
    public async Task PrepareAsync_WhenInstallationFails_DoesNotEvenTryToUpdate()
    {
        GivenInstallationReturns(Result<bool>.Failure(ErrorCode.Unexpected, "sem permissao"));

        await PrepareAsync();

        _reported.Should().Equal(ToolMaintenanceStatus.Installing, ToolMaintenanceStatus.UpdateUnavailable);
        _updater.Verify(updater => updater.UpdateAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class SynchronousProgress : IProgress<ToolMaintenanceStatus>
    {
        private readonly Action<ToolMaintenanceStatus> _onReport;

        public SynchronousProgress(Action<ToolMaintenanceStatus> onReport) => _onReport = onReport;

        public void Report(ToolMaintenanceStatus value) => _onReport(value);
    }
}
