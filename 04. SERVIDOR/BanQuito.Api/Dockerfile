FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["04. SERVIDOR/BanQuito.Api/BanQuito.Api.csproj", "04. SERVIDOR/BanQuito.Api/"]
COPY ["04. SERVIDOR/BanQuito.Dominio/BanQuito.Dominio.csproj", "04. SERVIDOR/BanQuito.Dominio/"]
COPY ["04. SERVIDOR/BanQuito.Contratos/BanQuito.Contratos.csproj", "04. SERVIDOR/BanQuito.Contratos/"]
RUN dotnet restore "04. SERVIDOR/BanQuito.Api/BanQuito.Api.csproj"
COPY . .
WORKDIR "/src/04. SERVIDOR/BanQuito.Api"
RUN dotnet publish "BanQuito.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:19101
EXPOSE 19101
ENTRYPOINT ["dotnet", "BanQuito.Api.dll"]

