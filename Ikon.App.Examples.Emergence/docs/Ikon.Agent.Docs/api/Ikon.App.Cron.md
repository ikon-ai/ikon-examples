namespace Ikon.App.Cron
  sealed record CronContext
    ctor(DateTime FireTimeUtc, string Schedule)
    static CronContext? Current { get; }
    DateTime FireTimeUtc { get; init; }
    string Schedule { get; init; }
    static IDisposable Use(CronContext context)
