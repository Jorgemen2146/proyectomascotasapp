using System.Diagnostics;
using DogPlatform.Notification.Application;
using Quartz;

namespace DogPlatform.Notification.API.Jobs;

[DisallowConcurrentExecution]
public sealed class VaccinationReminderJob(
    IVaccinationReminderRunner runner,
    ILogger<VaccinationReminderJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("VaccinationReminderJob started");
        try
        {
            var result = await runner.RunAsync(context.CancellationToken);
            logger.LogInformation(
                "VaccinationReminderJob completed. Health candidates={CandidateCount} Created={CreatedCount} Duplicates={DuplicateCount} Failed={FailedCount} DurationMs={DurationMs}",
                result.CandidateCount, result.CreatedCount, result.DuplicateCount,
                result.FailedCount, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "VaccinationReminderJob failed while requesting Health. DurationMs={DurationMs}",
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
