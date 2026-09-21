using Microsoft.EntityFrameworkCore;
using ProyectoClubCreativo.Data;
using ProyectoClubCreativo.Models.Entities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
// Conexión a ClubCreativoDB mediante Entity Framework Core
builder.Services.AddDbContext<ClubCreativoDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("ClubCreativoDB")));

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Crear el usuario administrador inicial si todavía no existe
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<ClubCreativoDbContext>();

    const string correoAdmin = "jossete.sanchez@clubcreativomivo.com";
   
    string? contrasenaAdmin =
    builder.Configuration["AdminInicial:Contrasena"];

    if (string.IsNullOrWhiteSpace(contrasenaAdmin))
    {
        throw new InvalidOperationException(
            "No se configuró la contraseña del administrador inicial."
        );
    }

    var administrador = await context.Usuarios
        .FirstOrDefaultAsync(u => u.Correo == correoAdmin);

    if (administrador == null)
    {
        var rolAdministrador = await context.Roles
            .FirstOrDefaultAsync(r =>
                r.Nombre == "Administrador" &&
                r.Activo);

        if (rolAdministrador != null)
        {
            administrador = new Usuario
            {
                Nombre = "Jossette",
                ApellidoPaterno = "Sanchez",
                ApellidoMaterno = "Aguilar",
                Correo = correoAdmin,
                Telefono = "71301389",
                IdProvincia = null,
                FechaNacimiento = null,
                ContrasenaHash =
                    BCrypt.Net.BCrypt.HashPassword(contrasenaAdmin),
                Estado = "Activo",
                FechaRegistro = DateTime.Now
            };

            context.Usuarios.Add(administrador);

            await context.SaveChangesAsync();

            var usuarioRol = new UsuarioRole
            {
                IdUsuario = administrador.IdUsuario,
                IdRol = rolAdministrador.IdRol,
                FechaAsignacion = DateTime.Now
            };

            context.UsuarioRoles.Add(usuarioRol);

            await context.SaveChangesAsync();
        }
    }
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
