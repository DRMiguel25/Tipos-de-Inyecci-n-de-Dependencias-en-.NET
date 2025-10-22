using OrdersApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Habilita OpenAPI (Swagger)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registrar el mismo servicio con tres ciclos de vida y claves
builder.Services.AddKeyedTransient<IOrderService, OrderService>("transient");
builder.Services.AddKeyedScoped<IOrderService, OrderService>("scoped");
builder.Services.AddKeyedSingleton<IOrderService, OrderService>("singleton");

var app = builder.Build();

// Configurar pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Añadir ruta raíz
app.MapGet("/", () => " si toy viendo esto es porque el Orders API esta en ejecución. si jalo esto cambiamos al  /swagger para ver los endpoints.");

// Mapear controladores (debe ir al final, pero antes de Run)
app.MapControllers();

// Iniciar la aplicación
app.Run();
