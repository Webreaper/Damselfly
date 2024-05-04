#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Damselfly.Web.Server/Damselfly.Web.Server.csproj", "Damselfly.Web.Server/"]
COPY ["Damselfly.Core.Constants/Damselfly.Core.Constants.csproj", "Damselfly.Core.Constants/"]
COPY ["Damselfly.Core.DbModels/Damselfly.Core.DbModels.csproj", "Damselfly.Core.DbModels/"]
COPY ["Damselfly.Core.Utils/Damselfly.Core.Utils.csproj", "Damselfly.Core.Utils/"]
COPY ["Damselfly.Shared.Utils/Damselfly.Shared.Utils.csproj", "Damselfly.Shared.Utils/"]
COPY ["Damselfly.Core.Interfaces/Damselfly.Core.Interfaces.csproj", "Damselfly.Core.Interfaces/"]
COPY ["Damselfly.Core/Damselfly.Core.csproj", "Damselfly.Core/"]
COPY ["Damselfly.ML.FaceONNX/Damselfly.ML.FaceONNX.csproj", "Damselfly.ML.FaceONNX/"]
COPY ["Damselfly.ML.ObjectDetection.ML/Damselfly.ML.ObjectDetection.csproj", "Damselfly.ML.ObjectDetection.ML/"]
COPY ["Damselfly.ML.ImageClassification/Damselfly.ML.ImageClassification.csproj", "Damselfly.ML.ImageClassification/"]
COPY ["Damselfly.Core.ScopedServices/Damselfly.Core.ScopedServices.csproj", "Damselfly.Core.ScopedServices/"]
COPY ["Damselfly.Core.ImageProcessing/Damselfly.Core.ImageProcessing.csproj", "Damselfly.Core.ImageProcessing/"]
COPY ["Damselfly.Migrations.Postgres/Damselfly.Migrations.Porstgres.csproj", "Damselfly.Migrations.Postgres/"]
RUN dotnet restore "./Damselfly.Web.Server/Damselfly.Web.Server.csproj"
COPY . .
WORKDIR "/src/Damselfly.Web.Server"
RUN dotnet build "./Damselfly.Web.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Damselfly.Web.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Damselfly.Web.Server.dll"]