FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY CaminoSeguro.Api/CaminoSeguro.Api.csproj CaminoSeguro.Api/
RUN dotnet restore CaminoSeguro.Api/CaminoSeguro.Api.csproj
COPY CaminoSeguro.Api/ CaminoSeguro.Api/
RUN dotnet publish CaminoSeguro.Api/CaminoSeguro.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "CaminoSeguro.Api.dll"]
