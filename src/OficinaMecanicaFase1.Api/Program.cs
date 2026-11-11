using OficinaMecanicaFase1.Application.AcessoInterno;
using OficinaMecanicaFase1.Application.Cadastros;
using OficinaMecanicaFase1.Application.Catalogo;
using OficinaMecanicaFase1.Application.OrdensServico;
using OficinaMecanicaFase1.Api.Security;
using OficinaMecanicaFase1.Infrastructure.Identity;
using OficinaMecanicaFase1.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
});
builder.Services.AddControllers(options =>
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .ConfigureApiBehaviorOptions(options => options.SuppressMapClientErrors = true);
builder.Services.AddOpenApi(options => options.AddDocumentTransformer(
    (documento, _, _) => EsquemaDeSegurancaOpenApi.AdicionarAsync(documento)));
builder.Services.AddSqlServerPersistence(builder.Configuration);
builder.Services.AddScoped<CadastroService>();
builder.Services.AddScoped<CatalogoService>();
builder.Services.AddScoped<OrdemServicoService>();
builder.Services.AddScoped<GestaoOrdemServicoService>();
builder.Services.AddScoped<DecisaoOrcamentoService>();
builder.Services.AddScoped<ReservaEstoqueService>();
builder.Services.AddScoped<ITentativaReservaEstoque>(services => services.GetRequiredService<ReservaEstoqueService>());
builder.Services.AddScoped<ExecucaoOrdemServicoService>();
builder.Services.AddScoped<AditivoService>();

var jwt = ConfiguracaoJwt.Ler(builder.Configuration);
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IEmissorTokenInterno, EmissorJwt>();
builder.Services.AddScoped<IIdentidadeInterna, IdentidadeInternaAdapter>();
builder.Services.AddScoped<AcessoInternoService>();

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
