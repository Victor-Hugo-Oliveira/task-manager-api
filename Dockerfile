# ---- Build stage: restore and publish with the full SDK ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only project files first so `dotnet restore` is cached until dependencies change.
COPY src/TaskManager.Api/TaskManager.Api.csproj src/TaskManager.Api/
COPY src/TaskManager.Application/TaskManager.Application.csproj src/TaskManager.Application/
COPY src/TaskManager.Domain/TaskManager.Domain.csproj src/TaskManager.Domain/
COPY src/TaskManager.Infrastructure/TaskManager.Infrastructure.csproj src/TaskManager.Infrastructure/
RUN dotnet restore src/TaskManager.Api/TaskManager.Api.csproj

COPY src/ src/
RUN dotnet publish src/TaskManager.Api/TaskManager.Api.csproj -c Release -o /app/publish --no-restore

# ---- Runtime stage: small ASP.NET image, non-root user ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "TaskManager.Api.dll"]
