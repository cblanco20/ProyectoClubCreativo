using Microsoft.EntityFrameworkCore;
using ProyectoClubCreativo.Data;

namespace ProyectoClubCreativo.Services
{
    public class ResultadoDescuento
    {
        public decimal Descuento { get; set; }
        public int? IdPromocion { get; set; }
        public string? NombrePromocion { get; set; }
    }

    public class PromocionesService
    {
        public const string Programada = "Programada";
        public const string Activa = "Activa";
        public const string Finalizada = "Finalizada";
        public const string Desactivada = "Desactivada";

        private readonly ClubCreativoDbContext _context;

        public PromocionesService(ClubCreativoDbContext context)
        {
            _context = context;
        }

        public static string CalcularEstado(DateTime inicio, DateTime fin, DateTime ahora)
        {
            if (ahora < inicio)
            {
                return Programada;
            }

            return ahora <= fin ? Activa : Finalizada;
        }

        public async Task SincronizarEstadosAsync()
        {
            DateTime ahora = DateTime.Now;

            await _context.Promociones
                .Where(p => p.Estado != Desactivada &&
                            p.Estado != Programada &&
                            p.FechaInicio > ahora)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Estado, Programada));

            await _context.Promociones
                .Where(p => p.Estado != Desactivada &&
                            p.Estado != Activa &&
                            p.FechaInicio <= ahora &&
                            p.FechaFin >= ahora)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Estado, Activa));

            await _context.Promociones
                .Where(p => p.Estado != Desactivada &&
                            p.Estado != Finalizada &&
                            p.FechaFin < ahora)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Estado, Finalizada));
        }

        public async Task<ResultadoDescuento> CalcularAsync(
            IEnumerable<(int IdProducto, decimal Precio, int Cantidad)> lineas)
        {
            List<(int IdProducto, decimal Precio, int Cantidad)> items = lineas.ToList();

            if (items.Count == 0)
            {
                return new ResultadoDescuento();
            }

            await SincronizarEstadosAsync();

            List<int> idsProductos = items.Select(i => i.IdProducto).Distinct().ToList();

            Dictionary<int, int> emprendimientoPorProducto = await _context.Productos
                .AsNoTracking()
                .Where(p => idsProductos.Contains(p.IdProducto))
                .Select(p => new { p.IdProducto, p.IdEmprendimiento })
                .ToDictionaryAsync(p => p.IdProducto, p => p.IdEmprendimiento);

            var promociones = await _context.Promociones
                .AsNoTracking()
                .Where(p => p.Estado == Activa)
                .Select(p => new
                {
                    p.IdPromocion,
                    p.Nombre,
                    p.TipoDescuento,
                    p.ValorDescuento,
                    p.LimiteUsos,
                    Usos = p.UsosPromocions.Count(),
                    Productos = p.IdProductos.Select(x => x.IdProducto).ToList(),
                    Emprendimientos = p.IdEmprendimientos.Select(x => x.IdEmprendimiento).ToList(),
                    Eventos = p.IdEventos.Count()
                })
                .ToListAsync();

            ResultadoDescuento mejor = new();

            foreach (var promocion in promociones)
            {
                if (promocion.LimiteUsos.HasValue && promocion.Usos >= promocion.LimiteUsos.Value)
                {
                    continue;
                }

                bool sinAsociaciones = promocion.Productos.Count == 0 &&
                                       promocion.Emprendimientos.Count == 0;

                if (!sinAsociaciones &&
                    promocion.Productos.Count == 0 &&
                    promocion.Emprendimientos.Count == 0)
                {
                    continue;
                }

                decimal baseAplicable = items
                    .Where(i =>
                        sinAsociaciones ||
                        promocion.Productos.Contains(i.IdProducto) ||
                        (emprendimientoPorProducto.TryGetValue(i.IdProducto, out int idEmprendimiento) &&
                         promocion.Emprendimientos.Contains(idEmprendimiento)))
                    .Sum(i => i.Precio * i.Cantidad);

                if (baseAplicable <= 0)
                {
                    continue;
                }

                decimal monto = promocion.TipoDescuento == "Porcentaje"
                    ? Math.Round(baseAplicable * promocion.ValorDescuento / 100m, 2)
                    : promocion.ValorDescuento;

                monto = Math.Min(monto, baseAplicable);

                if (monto > mejor.Descuento)
                {
                    mejor = new ResultadoDescuento
                    {
                        Descuento = monto,
                        IdPromocion = promocion.IdPromocion,
                        NombrePromocion = promocion.Nombre
                    };
                }
            }

            return mejor;
        }
    }

    public class PromocionesEstadoService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public PromocionesEstadoService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using IServiceScope scope = _scopeFactory.CreateScope();

                    PromocionesService servicio =
                        scope.ServiceProvider.GetRequiredService<PromocionesService>();

                    await servicio.SincronizarEstadosAsync();
                }
                catch (Exception)
                {
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
