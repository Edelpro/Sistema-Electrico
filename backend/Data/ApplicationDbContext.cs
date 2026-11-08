using Microsoft.EntityFrameworkCore;
using SistemaElectrico.Api.Models;

namespace SistemaElectrico.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<Rol> Roles => Set<Rol>();

    public DbSet<Cliente> Clientes => Set<Cliente>();

    public DbSet<Suministro> Suministros => Set<Suministro>();

    public DbSet<Medidor> Medidores => Set<Medidor>();

    public DbSet<Factura> Facturas => Set<Factura>();

    public DbSet<FacturaLinea> FacturaLineas => Set<FacturaLinea>();

    public DbSet<Pago> Pagos => Set<Pago>();

    public DbSet<PagoAsignacion> PagoAsignaciones => Set<PagoAsignacion>();

    public DbSet<OrdenTrabajo> OrdenesTrabajo => Set<OrdenTrabajo>();

    public DbSet<OrdenEstadoHistorial> OrdenesEstadoHistorial => Set<OrdenEstadoHistorial>();

    public DbSet<EvidenciaOrden> EvidenciasOrden => Set<EvidenciaOrden>();

    public DbSet<Cuadrilla> Cuadrillas => Set<Cuadrilla>();

    public DbSet<CuadrillaMiembro> CuadrillaMiembros => Set<CuadrillaMiembro>();

    public DbSet<Reclamo> Reclamos => Set<Reclamo>();

    public DbSet<ReclamoHistorial> ReclamoHistorial => Set<ReclamoHistorial>();

    public DbSet<EventoAuditoria> EventosAuditoria => Set<EventoAuditoria>();

    public DbSet<ConfiguracionSistema> ConfiguracionesSistema => Set<ConfiguracionSistema>();

    public DbSet<PoliticaSla> PoliticasSla => Set<PoliticaSla>();

    public DbSet<TokenRecuperacionPassword> TokensRecuperacionPassword => Set<TokenRecuperacionPassword>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Correo)
            .IsUnique();

        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Identificacion)
            .IsUnique();

        modelBuilder.Entity<Usuario>()
            .HasOne(u => u.Rol)
            .WithMany(r => r.Usuarios)
            .HasForeignKey(u => u.RolId);

        modelBuilder.Entity<TokenRecuperacionPassword>()
            .HasIndex(item => item.TokenHash)
            .IsUnique();
        modelBuilder.Entity<TokenRecuperacionPassword>()
            .HasOne(item => item.Usuario)
            .WithMany(usuario => usuario.TokensRecuperacion)
            .HasForeignKey(item => item.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TokenRecuperacionPassword>()
            .Property(item => item.TokenHash)
            .HasMaxLength(64);

        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.ClienteId)
            .IsUnique()
            .HasFilter("[ClienteId] IS NOT NULL");

        modelBuilder.Entity<Usuario>()
            .HasOne(u => u.Cliente)
            .WithOne(c => c.UsuarioPortal)
            .HasForeignKey<Usuario>(u => u.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Cliente>()
            .HasIndex(cliente => cliente.Identificacion)
            .IsUnique();

        modelBuilder.Entity<Cliente>()
            .Property(cliente => cliente.TipoIdentificacion)
            .HasMaxLength(20);

        modelBuilder.Entity<Cliente>()
            .Property(cliente => cliente.Identificacion)
            .HasMaxLength(20);

        modelBuilder.Entity<Cliente>()
            .Property(cliente => cliente.Nombres)
            .HasMaxLength(100);

        modelBuilder.Entity<Cliente>()
            .Property(cliente => cliente.Apellidos)
            .HasMaxLength(100);

        modelBuilder.Entity<Cliente>()
            .Property(cliente => cliente.Correo)
            .HasMaxLength(160);

        modelBuilder.Entity<Cliente>()
            .Property(cliente => cliente.Telefono)
            .HasMaxLength(20);

        modelBuilder.Entity<Cliente>()
            .Property(cliente => cliente.DireccionNotificacion)
            .HasMaxLength(300);

        modelBuilder.Entity<Suministro>()
            .HasIndex(suministro => suministro.NumeroSuministro)
            .IsUnique();

        modelBuilder.Entity<Suministro>()
            .HasIndex(suministro => suministro.NumeroContrato)
            .IsUnique();

        modelBuilder.Entity<Suministro>()
            .HasIndex(suministro => suministro.Cuen)
            .IsUnique();

        modelBuilder.Entity<Suministro>()
            .HasOne(suministro => suministro.ClienteTitular)
            .WithMany(cliente => cliente.SuministrosTitulares)
            .HasForeignKey(suministro => suministro.ClienteTitularId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Suministro>()
            .Property(suministro => suministro.NumeroSuministro)
            .HasMaxLength(30);

        modelBuilder.Entity<Suministro>()
            .Property(suministro => suministro.NumeroContrato)
            .HasMaxLength(30);

        modelBuilder.Entity<Suministro>()
            .Property(suministro => suministro.Cuen)
            .HasMaxLength(50);

        modelBuilder.Entity<Suministro>()
            .Property(suministro => suministro.DireccionServicio)
            .HasMaxLength(300);

        modelBuilder.Entity<Suministro>()
            .Property(suministro => suministro.Provincia)
            .HasMaxLength(80);

        modelBuilder.Entity<Suministro>()
            .Property(suministro => suministro.Canton)
            .HasMaxLength(80);

        modelBuilder.Entity<Suministro>()
            .Property(suministro => suministro.Parroquia)
            .HasMaxLength(80);

        modelBuilder.Entity<Suministro>()
            .Property(suministro => suministro.PlanGeocodigo)
            .HasMaxLength(50);

        modelBuilder.Entity<Suministro>()
            .Property(suministro => suministro.CodigoTarifa)
            .HasMaxLength(40);

        modelBuilder.Entity<Suministro>()
            .Property(suministro => suministro.NombreTarifa)
            .HasMaxLength(100);

        modelBuilder.Entity<Suministro>()
            .Property(suministro => suministro.EstadoServicio)
            .HasMaxLength(30);

        modelBuilder.Entity<Medidor>()
            .HasIndex(medidor => medidor.NumeroSerie)
            .IsUnique();

        modelBuilder.Entity<Medidor>()
            .HasIndex(medidor => new { medidor.SuministroId, medidor.Estado })
            .IsUnique()
            .HasFilter("[Estado] = 'ACTIVO'");

        modelBuilder.Entity<Medidor>()
            .HasOne(medidor => medidor.Suministro)
            .WithMany(suministro => suministro.Medidores)
            .HasForeignKey(medidor => medidor.SuministroId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Medidor>()
            .Property(medidor => medidor.NumeroSerie)
            .HasMaxLength(50);

        modelBuilder.Entity<Medidor>()
            .Property(medidor => medidor.TipoMedicion)
            .HasMaxLength(30);

        modelBuilder.Entity<Medidor>()
            .Property(medidor => medidor.Estado)
            .HasMaxLength(30);

        modelBuilder.Entity<Factura>()
            .HasIndex(factura => new { factura.SistemaOrigen, factura.IdentificadorExterno })
            .IsUnique();

        modelBuilder.Entity<Factura>()
            .HasIndex(factura => factura.NumeroFactura);

        modelBuilder.Entity<Factura>()
            .HasOne(factura => factura.Suministro)
            .WithMany(suministro => suministro.Facturas)
            .HasForeignKey(factura => factura.SuministroId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Factura>()
            .HasOne(factura => factura.Medidor)
            .WithMany(medidor => medidor.Facturas)
            .HasForeignKey(factura => factura.MedidorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Factura>()
            .Property(factura => factura.SistemaOrigen).HasMaxLength(80);
        modelBuilder.Entity<Factura>()
            .Property(factura => factura.IdentificadorExterno).HasMaxLength(100);
        modelBuilder.Entity<Factura>()
            .Property(factura => factura.NumeroFactura).HasMaxLength(60);
        modelBuilder.Entity<Factura>()
            .Property(factura => factura.NombreEmisor).HasMaxLength(160);
        modelBuilder.Entity<Factura>()
            .Property(factura => factura.RucEmisor).HasMaxLength(20);
        modelBuilder.Entity<Factura>()
            .Property(factura => factura.Moneda).HasMaxLength(3);
        modelBuilder.Entity<Factura>()
            .Property(factura => factura.Estado).HasMaxLength(30);
        modelBuilder.Entity<Factura>()
            .Property(factura => factura.MetodoLectura).HasMaxLength(30);
        modelBuilder.Entity<Factura>()
            .Property(factura => factura.UnidadConsumo).HasMaxLength(12);

        foreach (var propiedad in new[]
        {
            nameof(Factura.LecturaAnterior), nameof(Factura.LecturaActual),
            nameof(Factura.ConsumoFacturado), nameof(Factura.SubtotalServicioElectrico),
            nameof(Factura.SubtotalAlumbradoPublico), nameof(Factura.OtrosCargos),
            nameof(Factura.SaldoAnterior), nameof(Factura.RecaudacionTerceros),
            nameof(Factura.Subsidios), nameof(Factura.TotalFactura),
            nameof(Factura.SaldoPendiente)
        })
            modelBuilder.Entity<Factura>().Property(propiedad).HasPrecision(14, 2);

        modelBuilder.Entity<FacturaLinea>()
            .HasIndex(linea => new { linea.FacturaId, linea.NumeroLinea })
            .IsUnique();

        modelBuilder.Entity<FacturaLinea>()
            .HasOne(linea => linea.Factura)
            .WithMany(factura => factura.Lineas)
            .HasForeignKey(linea => linea.FacturaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FacturaLinea>()
            .Property(linea => linea.CodigoOriginal).HasMaxLength(50);
        modelBuilder.Entity<FacturaLinea>()
            .Property(linea => linea.DescripcionOriginal).HasMaxLength(200);
        modelBuilder.Entity<FacturaLinea>()
            .Property(linea => linea.Categoria).HasMaxLength(40);
        modelBuilder.Entity<FacturaLinea>()
            .Property(linea => linea.Monto).HasPrecision(14, 2);

        modelBuilder.Entity<Pago>().HasIndex(pago => new { pago.Proveedor, pago.EventoProveedorId }).IsUnique();
        modelBuilder.Entity<Pago>().HasOne(pago => pago.Suministro).WithMany(s => s.Pagos)
            .HasForeignKey(pago => pago.SuministroId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Pago>().Property(pago => pago.Proveedor).HasMaxLength(80);
        modelBuilder.Entity<Pago>().Property(pago => pago.EventoProveedorId).HasMaxLength(100);
        modelBuilder.Entity<Pago>().Property(pago => pago.Referencia).HasMaxLength(100);
        modelBuilder.Entity<Pago>().Property(pago => pago.Moneda).HasMaxLength(3);
        modelBuilder.Entity<Pago>().Property(pago => pago.Estado).HasMaxLength(30);
        modelBuilder.Entity<Pago>().Property(pago => pago.MotivoConciliacion).HasMaxLength(300);
        modelBuilder.Entity<Pago>().Property(pago => pago.Monto).HasPrecision(14, 2);

        modelBuilder.Entity<PagoAsignacion>().HasIndex(item => new { item.PagoId, item.FacturaId }).IsUnique();
        modelBuilder.Entity<PagoAsignacion>().HasOne(item => item.Pago).WithMany(p => p.Asignaciones)
            .HasForeignKey(item => item.PagoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PagoAsignacion>().HasOne(item => item.Factura).WithMany(f => f.AsignacionesPago)
            .HasForeignKey(item => item.FacturaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PagoAsignacion>().Property(item => item.MontoAplicado).HasPrecision(14, 2);

        modelBuilder.Entity<OrdenTrabajo>().HasIndex(orden => orden.Codigo).IsUnique();
        modelBuilder.Entity<OrdenTrabajo>().HasIndex(orden => new { orden.SuministroId, orden.Tipo, orden.Causa })
            .IsUnique().HasFilter("[Activa] = 1");
        modelBuilder.Entity<OrdenTrabajo>().HasOne(orden => orden.Suministro).WithMany(s => s.OrdenesTrabajo)
            .HasForeignKey(orden => orden.SuministroId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OrdenTrabajo>().HasOne(orden => orden.Pago).WithMany(p => p.OrdenesGeneradas)
            .HasForeignKey(orden => orden.PagoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OrdenTrabajo>().HasOne(orden => orden.TecnicoUsuario).WithMany()
            .HasForeignKey(orden => orden.TecnicoUsuarioId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OrdenTrabajo>().HasOne(orden => orden.Cuadrilla).WithMany(c => c.Ordenes)
            .HasForeignKey(orden => orden.CuadrillaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OrdenTrabajo>().Property(orden => orden.Codigo).HasMaxLength(40);
        modelBuilder.Entity<OrdenTrabajo>().Property(orden => orden.Tipo).HasMaxLength(20);
        modelBuilder.Entity<OrdenTrabajo>().Property(orden => orden.Causa).HasMaxLength(100);
        modelBuilder.Entity<OrdenTrabajo>().Property(orden => orden.Estado).HasMaxLength(30);
        modelBuilder.Entity<OrdenTrabajo>().Property(orden => orden.Prioridad).HasMaxLength(20);
        modelBuilder.Entity<OrdenTrabajo>().Property(orden => orden.Observacion).HasMaxLength(500);

        modelBuilder.Entity<OrdenEstadoHistorial>().HasOne(item => item.OrdenTrabajo).WithMany(o => o.Historial)
            .HasForeignKey(item => item.OrdenTrabajoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OrdenEstadoHistorial>().HasOne(item => item.ActorUsuario).WithMany()
            .HasForeignKey(item => item.ActorUsuarioId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OrdenEstadoHistorial>().Property(item => item.EstadoAnterior).HasMaxLength(30);
        modelBuilder.Entity<OrdenEstadoHistorial>().Property(item => item.EstadoNuevo).HasMaxLength(30);
        modelBuilder.Entity<OrdenEstadoHistorial>().Property(item => item.Motivo).HasMaxLength(300);

        modelBuilder.Entity<EvidenciaOrden>().HasOne(item => item.OrdenTrabajo).WithMany(o => o.Evidencias)
            .HasForeignKey(item => item.OrdenTrabajoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<EvidenciaOrden>().HasOne(item => item.TecnicoUsuario).WithMany(u => u.EvidenciasTecnicas)
            .HasForeignKey(item => item.TecnicoUsuarioId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<EvidenciaOrden>().Property(item => item.Latitud).HasPrecision(10, 7);
        modelBuilder.Entity<EvidenciaOrden>().Property(item => item.Longitud).HasPrecision(10, 7);
        modelBuilder.Entity<EvidenciaOrden>().Property(item => item.PrecisionMetros).HasPrecision(10, 2);
        modelBuilder.Entity<EvidenciaOrden>().Property(item => item.LecturaMedidor).HasPrecision(18, 3);
        modelBuilder.Entity<EvidenciaOrden>().Property(item => item.Observacion).HasMaxLength(1000);
        modelBuilder.Entity<EvidenciaOrden>().Property(item => item.NombreOriginal).HasMaxLength(255);
        modelBuilder.Entity<EvidenciaOrden>().Property(item => item.ArchivoAlmacenado).HasMaxLength(100);
        modelBuilder.Entity<EvidenciaOrden>().Property(item => item.TipoContenido).HasMaxLength(100);
        modelBuilder.Entity<EvidenciaOrden>().Property(item => item.HashSha256).HasMaxLength(64);

        modelBuilder.Entity<Cuadrilla>().HasIndex(item => item.Codigo).IsUnique();
        modelBuilder.Entity<Cuadrilla>().Property(item => item.Codigo).HasMaxLength(30);
        modelBuilder.Entity<Cuadrilla>().Property(item => item.Nombre).HasMaxLength(120);
        modelBuilder.Entity<Cuadrilla>().Property(item => item.Zona).HasMaxLength(120);
        modelBuilder.Entity<Cuadrilla>().Property(item => item.Estado).HasMaxLength(30);
        modelBuilder.Entity<Cuadrilla>().Property(item => item.Observaciones).HasMaxLength(500);

        modelBuilder.Entity<CuadrillaMiembro>().HasIndex(item => item.UsuarioId)
            .IsUnique().HasFilter("[Activo] = 1");
        modelBuilder.Entity<CuadrillaMiembro>().HasIndex(item => new { item.CuadrillaId, item.UsuarioId, item.Activo }).IsUnique();
        modelBuilder.Entity<CuadrillaMiembro>().HasOne(item => item.Cuadrilla).WithMany(c => c.Miembros)
            .HasForeignKey(item => item.CuadrillaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CuadrillaMiembro>().HasOne(item => item.Usuario).WithMany()
            .HasForeignKey(item => item.UsuarioId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Reclamo>().HasIndex(item => item.NumeroCaso).IsUnique();
        modelBuilder.Entity<Reclamo>().HasOne(item => item.Suministro).WithMany(s => s.Reclamos)
            .HasForeignKey(item => item.SuministroId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Reclamo>().HasOne(item => item.Pago).WithMany(p => p.Reclamos)
            .HasForeignKey(item => item.PagoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Reclamo>().HasOne(item => item.OrdenTrabajo).WithMany(o => o.Reclamos)
            .HasForeignKey(item => item.OrdenTrabajoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Reclamo>().HasOne(item => item.CreadoPorUsuario).WithMany()
            .HasForeignKey(item => item.CreadoPorUsuarioId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Reclamo>().HasOne(item => item.AsignadoAUsuario).WithMany()
            .HasForeignKey(item => item.AsignadoAUsuarioId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Reclamo>().Property(item => item.NumeroCaso).HasMaxLength(40);
        modelBuilder.Entity<Reclamo>().Property(item => item.Tipo).HasMaxLength(40);
        modelBuilder.Entity<Reclamo>().Property(item => item.Estado).HasMaxLength(30);
        modelBuilder.Entity<Reclamo>().Property(item => item.Prioridad).HasMaxLength(20);
        modelBuilder.Entity<Reclamo>().Property(item => item.Descripcion).HasMaxLength(1200);
        modelBuilder.Entity<Reclamo>().Property(item => item.Resolucion).HasMaxLength(1200);

        modelBuilder.Entity<ReclamoHistorial>().HasOne(item => item.Reclamo).WithMany(r => r.Historial)
            .HasForeignKey(item => item.ReclamoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ReclamoHistorial>().HasOne(item => item.ActorUsuario).WithMany()
            .HasForeignKey(item => item.ActorUsuarioId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ReclamoHistorial>().Property(item => item.EstadoAnterior).HasMaxLength(30);
        modelBuilder.Entity<ReclamoHistorial>().Property(item => item.EstadoNuevo).HasMaxLength(30);
        modelBuilder.Entity<ReclamoHistorial>().Property(item => item.Motivo).HasMaxLength(500);

        modelBuilder.Entity<EventoAuditoria>()
            .HasOne(evento => evento.ActorUsuario)
            .WithMany()
            .HasForeignKey(evento => evento.ActorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EventoAuditoria>()
            .Property(evento => evento.Accion)
            .HasMaxLength(80);

        modelBuilder.Entity<EventoAuditoria>()
            .Property(evento => evento.TipoEntidad)
            .HasMaxLength(80);

        modelBuilder.Entity<EventoAuditoria>()
            .Property(evento => evento.EntidadId)
            .HasMaxLength(80);

        modelBuilder.Entity<EventoAuditoria>()
            .Property(evento => evento.Motivo)
            .HasMaxLength(300);

        modelBuilder.Entity<EventoAuditoria>()
            .Property(evento => evento.ValorAnteriorJson)
            .HasMaxLength(4000);

        modelBuilder.Entity<EventoAuditoria>()
            .Property(evento => evento.ValorNuevoJson)
            .HasMaxLength(4000);

        modelBuilder.Entity<EventoAuditoria>()
            .HasIndex(evento => evento.OcurridoEnUtc);

        modelBuilder.Entity<ConfiguracionSistema>().Property(x => x.NombreEmpresa).HasMaxLength(160);
        modelBuilder.Entity<ConfiguracionSistema>().Property(x => x.Ruc).HasMaxLength(20);
        modelBuilder.Entity<ConfiguracionSistema>().Property(x => x.CorreoSoporte).HasMaxLength(160);
        modelBuilder.Entity<ConfiguracionSistema>().Property(x => x.TelefonoSoporte).HasMaxLength(30);
        modelBuilder.Entity<ConfiguracionSistema>().Property(x => x.Direccion).HasMaxLength(300);
        modelBuilder.Entity<ConfiguracionSistema>().Property(x => x.ZonaHoraria).HasMaxLength(80);
        modelBuilder.Entity<ConfiguracionSistema>().HasOne(x => x.ActualizadoPorUsuario).WithMany()
            .HasForeignKey(x => x.ActualizadoPorUsuarioId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PoliticaSla>().HasIndex(x => new { x.TipoOrden, x.TipoZona, x.Version }).IsUnique();
        modelBuilder.Entity<PoliticaSla>().HasIndex(x => new { x.TipoOrden, x.TipoZona, x.Activa })
            .IsUnique().HasFilter("[Activa] = 1");
        modelBuilder.Entity<PoliticaSla>().Property(x => x.TipoOrden).HasMaxLength(30);
        modelBuilder.Entity<PoliticaSla>().Property(x => x.TipoZona).HasMaxLength(20);
        modelBuilder.Entity<PoliticaSla>().Property(x => x.Motivo).HasMaxLength(300);
        modelBuilder.Entity<PoliticaSla>().HasOne(x => x.CreadoPorUsuario).WithMany()
            .HasForeignKey(x => x.CreadoPorUsuarioId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ConfiguracionSistema>().HasData(new ConfiguracionSistema
        {
            Id = 1,
            NombreEmpresa = "Empresa Eléctrica",
            ZonaHoraria = "America/Guayaquil",
            TamanoMaximoEvidenciaMb = 5,
            FechaActualizacionUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        modelBuilder.Entity<PoliticaSla>().HasData(
            new PoliticaSla { Id = 1, Version = 1, TipoOrden = "RECONEXION", TipoZona = "URBANA",
                HorasObjetivo = 10, HoraInicioLaboral = new TimeSpan(8, 0, 0), HoraFinLaboral = new TimeSpan(17, 0, 0),
                IncluyeFinDeSemana = false, VigenteDesdeUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Activa = true, Motivo = "Política inicial según referencia del SRS." },
            new PoliticaSla { Id = 2, Version = 1, TipoOrden = "RECONEXION", TipoZona = "RURAL",
                HorasObjetivo = 24, HoraInicioLaboral = new TimeSpan(8, 0, 0), HoraFinLaboral = new TimeSpan(17, 0, 0),
                IncluyeFinDeSemana = false, VigenteDesdeUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Activa = true, Motivo = "Política inicial según referencia del SRS." });

        modelBuilder.Entity<Rol>().HasData(
            new Rol
            {
                Id = 1,
                Nombre = "Administrador",
                Descripcion = "Administración general del sistema",
                Activo = true
            },
            new Rol
            {
                Id = 2,
                Nombre = "Tecnico",
                Descripcion = "Gestión de órdenes técnicas",
                Activo = true
            },
            new Rol
            {
                Id = 3,
                Nombre = "Usuario",
                Descripcion = "Cliente del servicio eléctrico",
                Activo = true
            }
        );
    }
}
