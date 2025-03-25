FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BrainStormEra.csproj", "./"]
RUN dotnet restore "./BrainStormEra.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet publish "./BrainStormEra.csproj" -c Release -o /app/publish
FROM base AS final
WORKDIR /app
USER root
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BrainStormEra.dll"]
