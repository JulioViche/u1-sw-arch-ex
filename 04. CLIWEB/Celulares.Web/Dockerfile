FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["04. CLIWEB/Celulares.Web/Celulares.Web.csproj", "04. CLIWEB/Celulares.Web/"]
COPY ["04. COMPARTIDO/Celulares.Cliente/Celulares.Cliente.csproj", "04. COMPARTIDO/Celulares.Cliente/"]
COPY ["04. SERVIDOR/Contratos/Celulares.Contratos.csproj", "04. SERVIDOR/Contratos/"]
RUN dotnet restore "04. CLIWEB/Celulares.Web/Celulares.Web.csproj"
COPY . .
WORKDIR "/src/04. CLIWEB/Celulares.Web"
RUN dotnet publish "Celulares.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:19103
EXPOSE 19103
ENTRYPOINT ["dotnet", "Celulares.Web.dll"]

