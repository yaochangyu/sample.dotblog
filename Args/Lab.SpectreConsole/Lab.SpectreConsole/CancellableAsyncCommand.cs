using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

namespace Lab.SpectreConsole;

public abstract class CancellableAsyncCommand<TSettings> : AsyncCommand<TSettings>
    where TSettings : CommandSettings
{
    private readonly ILogger<CancellableAsyncCommand<TSettings>> _logger;

    protected CancellableAsyncCommand(ILogger<CancellableAsyncCommand<TSettings>> logger)
    {
        this._logger = logger;
    }

    public abstract Task<int> ExecuteAsync(CommandContext context, TSettings settings, CancellationToken cancellation);

    public override async Task<int> ExecuteAsync(CommandContext context, TSettings settings)
    {
        using var cancellationSource = new CancellationTokenSource();

        Console.CancelKeyPress += OnCancelKeyPress;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        using var _ = cancellationSource.Token.Register(
            () =>
            {
                AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
                Console.CancelKeyPress -= OnCancelKeyPress;
            }
        );
        int exitCode = -1;
        try
        {
            this._logger.LogInformation("執行任務中...");
            var executeTask = this.ExecuteAsync(context, settings, cancellationSource.Token);
            exitCode = await executeTask;
            this._logger.LogInformation("執行完成!!!");
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            Console.CancelKeyPress -= OnCancelKeyPress;
        }
        catch (OperationCanceledException)
        {
            exitCode = 0;
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "執行命令時發生非預期的錯誤");
        }

        return exitCode;

        void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            // NOTE: cancel event, don't terminate the process
            e.Cancel = true;

            cancellationSource.Cancel();
        }

        void OnProcessExit(object? sender, EventArgs e)
        {
            if (cancellationSource.IsCancellationRequested)
            {
                // NOTE: SIGINT (cancel key was pressed, this shouldn't ever actually hit however, as we remove the event handler upon cancellation of the `cancellationSource`)
                return;
            }

            cancellationSource.Cancel();
        }
    }
}