FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "Inventory System/Inventory System.csproj"
RUN dotnet publish "Inventory System/Inventory System.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENTRYPOINT ["dotnet", "Inventory_System.dll"]
