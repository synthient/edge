FROM mcr.microsoft.com/dotnet/sdk:10.0 AS publish
WORKDIR /src
COPY ["src/Synthient.Edge.csproj", "src/"]
RUN dotnet restore "src/Synthient.Edge.csproj"
COPY . .
RUN dotnet publish "src/Synthient.Edge.csproj" \
    -c Release --no-restore -o /app/publish \
    /p:UseAppHost=false \
    
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
USER $APP_UID
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Synthient.Edge.dll"]
CMD ["/app/config.yml"]