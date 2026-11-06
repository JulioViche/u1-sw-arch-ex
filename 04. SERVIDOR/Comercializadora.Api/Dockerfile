FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["04. SERVIDOR/Comercializadora.Api/Comercializadora.Api.csproj", "04. SERVIDOR/Comercializadora.Api/"]
COPY ["04. SERVIDOR/Comercializadora.Dominio/Comercializadora.Dominio.csproj", "04. SERVIDOR/Comercializadora.Dominio/"]
COPY ["04. SERVIDOR/Contratos/Celulares.Contratos.csproj", "04. SERVIDOR/Contratos/"]
RUN dotnet restore "04. SERVIDOR/Comercializadora.Api/Comercializadora.Api.csproj"
COPY . .
WORKDIR "/src/04. SERVIDOR/Comercializadora.Api"
RUN dotnet publish "Comercializadora.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:19102
EXPOSE 19102
ENTRYPOINT ["dotnet", "Comercializadora.Api.dll"]

