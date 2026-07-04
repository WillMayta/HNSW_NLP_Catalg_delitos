using MinisterioPublico.Domain.Entities;
using MinisterioPublico.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MinisterioPublico.API;

/// <summary>
/// Datos semilla mínimos para que el sistema sea utilizable en una primera
/// ejecución: roles base, familias delictivas iniciales (según títulos del
/// Código Penal peruano) y un usuario administrador.
///
/// IMPORTANTE: la contraseña por defecto debe cambiarse inmediatamente en
/// un entorno real. Aquí se usa únicamente para fines de demostración.
/// </summary>
public static class SeedData
{
    public static async Task EjecutarAsync(MinisterioPublicoDbContext db)
    {
        if (!await db.Roles.AnyAsync())
        {
            db.Roles.AddRange(
                new Rol { Nombre = "Administrador", Descripcion = "Acceso total al sistema." },
                new Rol { Nombre = "EspecialistaJuridico", Descripcion = "Valida propuestas y consolida el catálogo penal." },
                new Rol { Nombre = "Analista", Descripcion = "Carga datos, ejecuta normalización y agrupamiento." },
                new Rol { Nombre = "Consulta", Descripcion = "Solo lectura: búsqueda y dashboard." }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.FamiliasDelictivas.AnyAsync())
        {
            db.FamiliasDelictivas.AddRange(
                new FamiliaDelictiva { Nombre = "Delitos contra la vida, el cuerpo y la salud", TituloCodigoPenal = "Título I" },
                new FamiliaDelictiva { Nombre = "Delitos contra la libertad", TituloCodigoPenal = "Título IV" },
                new FamiliaDelictiva { Nombre = "Delitos contra el patrimonio", TituloCodigoPenal = "Título V" },
                new FamiliaDelictiva { Nombre = "Delitos contra la familia", TituloCodigoPenal = "Título III" },
                new FamiliaDelictiva { Nombre = "Delitos contra la fe pública", TituloCodigoPenal = "Título XIX" },
                new FamiliaDelictiva { Nombre = "Delitos contra la administración pública", TituloCodigoPenal = "Título XVIII" }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.Usuarios.AnyAsync())
        {
            var admin = new Usuario
            {
                NombreUsuario = "admin",
                NombreCompleto = "Administrador del Sistema",
                Correo = "admin@mpfn.gob.pe",
                HashContrasena = BCrypt.Net.BCrypt.HashPassword("Admin#2026"),
            };
            db.Usuarios.Add(admin);
            await db.SaveChangesAsync();

            var rolAdmin = await db.Roles.FirstAsync(r => r.Nombre == "Administrador");
            db.UsuarioRoles.Add(new UsuarioRol { UsuarioId = admin.Id, RolId = rolAdmin.Id });
            await db.SaveChangesAsync();
        }
    }
}
