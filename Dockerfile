# Stage 1: build the web project with the full .NET SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY Prahelika.Calculators/ Prahelika.Calculators/
RUN dotnet publish Prahelika.Calculators/src/Prahelika.Calculators.Web/Prahelika.Calculators.Web.csproj --configuration Release --output /app

# Stage 2: run it on the smaller ASP.NET runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .

# Render sends traffic to port 10000 by default
ENV ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "Prahelika.Calculators.Web.dll"]