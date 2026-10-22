FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY src/ ./src/
RUN dotnet publish src/WitcherKM.Workflow.Api/WitcherKM.Workflow.Api.csproj \
    -c Release -o /publish -p:NuGetAudit=false

FROM build AS test
COPY tests/ImageCacheSmoke.csproj tests/ImageCacheSmoke.cs ./tests/
RUN dotnet restore tests/ImageCacheSmoke.csproj -p:NuGetAudit=false \
    && dotnet run -c Release --no-restore --project tests/ImageCacheSmoke.csproj

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
RUN mkdir -p /app/App_Data && chown $APP_UID:$APP_UID /app/App_Data
COPY --from=build /publish/ ./
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "WitcherKM.Workflow.Api.dll"]
