﻿FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Dwight.csproj", "./"]
COPY ["nuget.config", "./"]
RUN dotnet restore "Dwight.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "Dwight.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Dwight.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Dwight.dll"]
