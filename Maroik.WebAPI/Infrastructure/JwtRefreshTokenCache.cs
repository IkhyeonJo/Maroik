namespace Maroik.WebAPI.Infrastructure
{
    public class JwtRefreshTokenCache(IJwtAuthManager jwtAuthManager) : IHostedService, IDisposable
    {
        private Timer _timer;

        public Task StartAsync(CancellationToken stoppingToken)
        {
            // remove expired refresh tokens from the cache every minute
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            jwtAuthManager.RemoveExpiredRefreshTokens(DateTime.UtcNow);
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _ = (_timer?.Change(Timeout.Infinite, 0));
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
