FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY OficinaMecanicaFase1.slnx ./
COPY src/OficinaMecanicaFase1.Domain/*.csproj src/OficinaMecanicaFase1.Domain/
COPY src/OficinaMecanicaFase1.Application/*.csproj src/OficinaMecanicaFase1.Application/
COPY src/OficinaMecanicaFase1.Infrastructure/*.csproj src/OficinaMecanicaFase1.Infrastructure/
COPY src/OficinaMecanicaFase1.Api/*.csproj src/OficinaMecanicaFase1.Api/
RUN dotnet restore src/OficinaMecanicaFase1.Api/OficinaMecanicaFase1.Api.csproj
COPY src/ ./src/
RUN dotnet publish src/OficinaMecanicaFase1.Api/OficinaMecanicaFase1.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "OficinaMecanicaFase1.Api.dll"]
