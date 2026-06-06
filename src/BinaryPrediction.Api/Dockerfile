FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/BinaryPrediction.Api/BinaryPrediction.Api.csproj", "src/BinaryPrediction.Api/"]
COPY ["src/BinaryPrediction.Core/BinaryPrediction.Core.csproj", "src/BinaryPrediction.Core/"]
COPY ["src/BinaryPrediction.Infrastructure/BinaryPrediction.Infrastructure.csproj", "src/BinaryPrediction.Infrastructure/"]
RUN dotnet restore "src/BinaryPrediction.Api/BinaryPrediction.Api.csproj"
COPY . .
WORKDIR "/src/src/BinaryPrediction.Api"
RUN dotnet build "BinaryPrediction.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BinaryPrediction.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BinaryPrediction.Api.dll"]
