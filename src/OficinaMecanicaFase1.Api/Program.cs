using OficinaMecanicaFase1.Api.Contracts;
using OficinaMecanicaFase1.Api.Cadastros;
using OficinaMecanicaFase1.Api.Catalogo;
using OficinaMecanicaFase1.Api.OrdensServico;
using OficinaMecanicaFase1.Api.Security;
using OficinaMecanicaFase1.Infrastructure.Identity;
using OficinaMecanicaFase1.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
});
builder.Services.AddOpenApi(options => options.AddDocumentTransformer(
    (documento, _, _) => EsquemaDeSegurancaOpenApi.AdicionarAsync(documento)));
builder.Services.AddSqlServerPersistence(builder.Configuration);

var jwt = ConfiguracaoJwt.Ler(builder.Configuration);
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<EmissorJwt>();

builder.Services.AddIdentityCore<UsuarioInterno>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<IdentidadeDbContext>()
    .AddDefaultTokenProviders()
    .AddSignInManager();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Emissor,
            ValidateAudience = true,
            ValidAudience = jwt.Publico,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwt.Chave),
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "unique_name",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("EquipeInterna", policy => policy.RequireAuthenticatedUser().RequireRole(PapeisInternos.Todos))
    .AddPolicy("Cadastros", policy => policy.RequireAuthenticatedUser().RequireRole(
        PapeisInternos.Administrador, PapeisInternos.Atendente))
    .AddPolicy("AtendentesEMecanicos", policy => policy.RequireAuthenticatedUser().RequireRole(
        PapeisInternos.Atendente, PapeisInternos.Mecanico))
    .AddPolicy("Mecanicos", policy => policy.RequireAuthenticatedUser().RequireRole(PapeisInternos.Mecanico))
    .AddPolicy("Atendentes", policy => policy.RequireAuthenticatedUser().RequireRole(PapeisInternos.Atendente))
    .AddPolicy("Estoquistas", policy => policy.RequireAuthenticatedUser().RequireRole(PapeisInternos.Estoquista))
    .AddPolicy("Administradores", policy => policy.RequireAuthenticatedUser().RequireRole(PapeisInternos.Administrador));

var app = builder.Build();
var modoRedefinicaoAdministrador = string.Equals(
    Environment.GetEnvironmentVariable("OFICINA_REDEFINIR_ADMIN"), "1", StringComparison.Ordinal);
var modoLimpezaUsuariosTeste = string.Equals(
    Environment.GetEnvironmentVariable("OFICINA_LIMPAR_USUARIOS_TESTE"), "1", StringComparison.Ordinal);
var modoManutencaoIdentidade = modoRedefinicaoAdministrador || modoLimpezaUsuariosTeste;

try
{
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<OficinaDbContext>();
        await db.Database.MigrateAsync();
        var identidade = scope.ServiceProvider.GetRequiredService<IdentidadeDbContext>();
        await identidade.Database.MigrateAsync();
        await InicializadorIdentidade.InicializarAsync(scope.ServiceProvider, app.Configuration);
    }

    if (modoLimpezaUsuariosTeste)
    {
        await using var scope = app.Services.CreateAsyncScope();
        await LimpezaUsuariosTeste.ExecutarAsync(scope.ServiceProvider);
        await InicializadorIdentidade.InicializarAsync(scope.ServiceProvider, app.Configuration);
        await RedefinidorSenhaAdministrador.ExecutarAsync(scope.ServiceProvider);
        return;
    }

    if (modoRedefinicaoAdministrador)
    {
        await using var scope = app.Services.CreateAsyncScope();
        await RedefinidorSenhaAdministrador.ExecutarAsync(scope.ServiceProvider);
        return;
    }
}
catch (Exception exception) when (modoManutencaoIdentidade)
{
    Console.Error.WriteLine("Falha ao preparar o banco ou redefinir a senha: " + exception.GetBaseException().Message);
    Environment.ExitCode = 1;
    return;
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Oficina Mecânica API v1"));
}

app.MapGet("/api/saude", () => TypedResults.Ok(new HealthResponse("operacional")))
    .WithName("ConsultarSaude")
    .WithTags("Sistema");

app.MapGet("/api/saude/banco", async (OficinaDbContext db, CancellationToken cancellationToken) =>
        await db.Database.CanConnectAsync(cancellationToken)
            ? Results.Ok(new HealthResponse("operacional"))
            : Results.Problem("Banco de dados indisponível", statusCode: 503))
    .WithName("ConsultarSaudeBanco")
    .WithTags("Sistema");

app.MapearAcessoInterno();
app.MapearCadastros();
app.MapearCatalogoEstoque();
app.MapearOrdensServico();
app.MapearAvisosEstoque();
app.MapearClienteOrcamento();
app.MapearAditivos();

app.Run();

public partial class Program { }
