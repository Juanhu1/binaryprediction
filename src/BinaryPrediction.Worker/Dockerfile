FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/BinaryPrediction.Worker/BinaryPrediction.Worker.csproj", "src/BinaryPrediction.Worker/"]
COPY ["src/BinaryPrediction.Core/BinaryPrediction.Core.csproj", "src/BinaryPrediction.Core/"]
COPY ["src/BinaryPrediction.Infrastructure/BinaryPrediction.Infrastructure.csproj", "src/BinaryPrediction.Infrastructure/"]
RUN dotnet restore "src/BinaryPrediction.Worker/BinaryPrediction.Worker.csproj"
COPY . .
WORKDIR "/src/src/BinaryPrediction.Worker"
RUN dotnet build "BinaryPrediction.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BinaryPrediction.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BinaryPrediction.Worker.dll"]
