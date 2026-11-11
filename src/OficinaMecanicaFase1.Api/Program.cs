using OficinaMecanicaFase1.Api.DependencyInjection;
using OficinaMecanicaFase1.Api.Security;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOficinaMecanica(builder.Configuration);

var app = builder.Build();
if (!await InicializacaoAplicacao.DeveIniciarServidorAsync(app))
    return;

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Oficina Mecânica API v1"));
}

app.MapControllers();

app.Run();

public partial class Program { }
