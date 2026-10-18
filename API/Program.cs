using Abtracciones.Interfaces;
using Scalar.AspNetCore;
using DA;
using Flujo;
using Abtracciones.Modelos;
using DA.Contexto;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


//Inyeccion de dependencias
builder.Services.AddScoped<IClienteDA, ClienteDA>();
builder.Services.AddScoped<IClienteFlujo, ClienteFlujo>();

var cadenaConexion = builder.Configuration.GetConnectionString("SistemaSoporte") ??
    throw new InvalidOperationException("Falta la cadena de conexion 'Sistema Soporte' en la configuraion");

builder.Services.AddDbContext<SistemaSoporteContexto>(opciones => opciones.UseSqlServer(cadenaConexion));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/min/clientes", (IClienteFlujo flujo) => Results.Ok(flujo.Obtener()));

app.MapGet("/min/clientes/{id:int}", (int id, IClienteFlujo flujo) =>
{
    var cliente = flujo.ObtenerPorId(id);
    return cliente is null ? Results.NotFound() : Results.Ok(cliente);
});

app.MapPost("/min/clientes", (Cliente cliente, IClienteFlujo flujo) =>
{
    var creado = flujo.Agregar(cliente);
    return Results.Created($"/min/clientes/{creado.Id}",creado);
});


//nos permite que "/" sirva wwwroot/index,html sin escribir el nombre del archivo
app.UseDefaultFiles();

//Habilita brindar(servir) al servidor los archivos que viven en wwwroot (WebRoot)
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();
