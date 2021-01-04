FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /app
# copy csproj only so restored project will be cached
COPY PixelFlut.Demo/PixelFlut.Demo.csproj /app/PixelFlut.Demo/
COPY PixelFlut.Infrastructure/PixelFlut.Infrastructure.csproj /app/PixelFlut.Infrastructure/
RUN dotnet restore PixelFlut.Demo/PixelFlut.Demo.csproj
COPY PixelFlut.Demo /app/PixelFlut.Demo
COPY PixelFlut.Infrastructure /app/PixelFlut.Infrastructure
RUN dotnet publish -c Release PixelFlut.Demo/PixelFlut.Demo.csproj -o /app/build

FROM mcr.microsoft.com/dotnet/runtime:5.0
WORKDIR /app
RUN apt-get update
RUN apt-get install -y --allow-unauthenticated libc6-dev libgdiplus libx11-dev 
COPY --from=build /app/build/ ./
ENTRYPOINT ["dotnet", "PixelFlut.Demo.dll"]
