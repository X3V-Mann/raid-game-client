namespace GameApi.Services;

/// <summary>
/// Каждые 5 секунд прокачивает пассивное восстановление в убежище:
/// HP, выносливость и (при улучшениях) голод/жажду.
/// </summary>
public class ShelterRegenService : BackgroundService
{
    private readonly GameStateService _state;

    public ShelterRegenService(GameStateService state) => _state = state;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                _state.PassiveTick();
            }
        }
        catch (OperationCanceledException)
        {
            // штатное завершение
        }
    }
}
