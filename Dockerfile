FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-bionic AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

ENV ASPNETCORE_ENVIRONMENT=PRD
 
FROM mcr.microsoft.com/dotnet/core/sdk:3.1-bionic AS build
WORKDIR /build
 
COPY . .
 
RUN dotnet restore "src/services/API/MyProject.csproj"
 
WORKDIR "/build"
RUN dotnet build "src/services/API/MyProject.csproj" -c Release -o /app/build
 
FROM build AS publish
RUN dotnet publish "src/services/API/MyProject.csproj" -c Release -o /app/publish
 
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyProject.dll"]

RUN apt-get update \
    && apt-get install -y --allow-unauthenticated \
        libc6-dev \
        libgdiplus \
        libx11-dev \
     && rm -rf /var/lib/apt/lists/*
