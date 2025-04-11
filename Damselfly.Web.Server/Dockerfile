# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]
COPY ["NuGet.config", "."]
COPY ["Damselfly.Web.Server/Damselfly.Web.Server.csproj", "Damselfly.Web.Server/"]
COPY ["Damselfly.Core.Constants/Damselfly.Core.Constants.csproj", "Damselfly.Core.Constants/"]
COPY ["Damselfly.Core.DbModels/Damselfly.Core.DbModels.csproj", "Damselfly.Core.DbModels/"]
COPY ["Damselfly.Core.Utils/Damselfly.Core.Utils.csproj", "Damselfly.Core.Utils/"]
COPY ["Damselfly.Shared.Utils/Damselfly.Shared.Utils.csproj", "Damselfly.Shared.Utils/"]
COPY ["Damselfly.Core.Interfaces/Damselfly.Core.Interfaces.csproj", "Damselfly.Core.Interfaces/"]
COPY ["Damselfly.Core/Damselfly.Core.csproj", "Damselfly.Core/"]
COPY ["Damselfly.Core.ScopedServices/Damselfly.Core.ScopedServices.csproj", "Damselfly.Core.ScopedServices/"]
COPY ["Damselfly.PaymentProcessing/Damselfly.PaymentProcessing.csproj", "Damselfly.PaymentProcessing/"]
COPY ["Damselfly.Core.ImageProcessing/Damselfly.Core.ImageProcessing.csproj", "Damselfly.Core.ImageProcessing/"]
COPY ["Damselfly.Migrations.Postgres/Damselfly.Migrations.Postgres.csproj", "Damselfly.Migrations.Postgres/"]
RUN dotnet restore "./Damselfly.Web.Server/Damselfly.Web.Server.csproj"
COPY . .
WORKDIR "/src/Damselfly.Web.Server"
RUN dotnet build "./Damselfly.Web.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Damselfly.Web.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Damselfly.Web.Server.dll"]