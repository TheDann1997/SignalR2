using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SignalR.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SignalR.Services
{
    public class LimpiezaLlamadasService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LimpiezaLlamadasService> _logger;
        private readonly TimeSpan _intervalo = TimeSpan.FromDays(1); // Ejecutar cada día
        private readonly int _diasRetencion = 365; // Mantener datos por 1 año

        public LimpiezaLlamadasService(
            IServiceProvider serviceProvider,
            ILogger<LimpiezaLlamadasService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de limpieza de llamadas iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await LimpiarDatosAntiguos();
                    _logger.LogInformation("Limpieza de datos completada");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error durante la limpieza de datos");
                }

                // Esperar hasta la próxima ejecución
                await Task.Delay(_intervalo, stoppingToken);
            }
        }

        private async Task LimpiarDatosAntiguos()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var fechaLimite = DateTime.UtcNow.AddDays(-_diasRetencion);

            // Obtener IDs de llamadas antiguas
            var llamadasAntiguas = await context.LlamadasGrupales
                .Where(l => l.FechaFin.HasValue && l.FechaFin < fechaLimite)
                .Select(l => l.Id)
                .ToListAsync();

            if (llamadasAntiguas.Any())
            {
                // Eliminar participantes de llamadas antiguas
                var participantesEliminados = await context.ParticipantesLlamada
                    .Where(p => llamadasAntiguas.Contains(p.LlamadaGrupalId))
                    .ExecuteDeleteAsync();

                // Eliminar llamadas antiguas
                var llamadasEliminadas = await context.LlamadasGrupales
                    .Where(l => llamadasAntiguas.Contains(l.Id))
                    .ExecuteDeleteAsync();

                _logger.LogInformation(
                    "Limpieza completada: {LlamadasEliminadas} llamadas y {ParticipantesEliminados} participantes eliminados",
                    llamadasEliminadas, participantesEliminados);
            }
            else
            {
                _logger.LogInformation("No hay datos antiguos para limpiar");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Servicio de limpieza de llamadas detenido");
            await base.StopAsync(cancellationToken);
        }
    }
}