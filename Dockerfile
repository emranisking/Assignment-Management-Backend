# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first for restore caching
COPY AssignmentManagement.sln ./
COPY AssignmentManagement.Common/AssignmentManagement.Common.csproj AssignmentManagement.Common/
COPY AssignmentManagement.Domain/AssignmentManagement.Domain.csproj AssignmentManagement.Domain/
COPY AssignmentManagement.Application/AssignmentManagement.Application.csproj AssignmentManagement.Application/
COPY AssignmentManagement.Infrastructure/AssignmentManagement.Infrastructure.csproj AssignmentManagement.Infrastructure/
COPY AssignmentManagement.API/AssignmentManagement.API.csproj AssignmentManagement.API/
COPY AssignmentManagement.Tests/AssignmentManagement.Tests.csproj AssignmentManagement.Tests/

RUN dotnet restore AssignmentManagement.API/AssignmentManagement.API.csproj

# Copy the rest and publish
COPY . .
RUN dotnet publish AssignmentManagement.API/AssignmentManagement.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
# Persist uploaded submissions here (mounted as a volume in docker-compose)
RUN mkdir -p /app/storage
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "AssignmentManagement.API.dll"]
