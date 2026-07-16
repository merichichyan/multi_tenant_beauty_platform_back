# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["multi_tenant_beauty_platform_back.csproj", "./"]
RUN dotnet restore "multi_tenant_beauty_platform_back.csproj"

# Copy all source files and build
COPY . .
RUN dotnet build "multi_tenant_beauty_platform_back.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "multi_tenant_beauty_platform_back.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Expose the default ASP.NET Core port
EXPOSE 8080
EXPOSE 8081

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "multi_tenant_beauty_platform_back.dll"]
