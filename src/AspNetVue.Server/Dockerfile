# syntax=docker/dockerfile:1
# Multi-stage: build Vue (outDir -> ../AspNetVue.Server/wwwroot) lalu publish ASP.NET.

FROM oven/bun:1-alpine AS client-build
WORKDIR /src/src/AspNetVue.Client
COPY src/AspNetVue.Client/package.json src/AspNetVue.Client/bun.lock* ./
RUN bun install
COPY src/AspNetVue.Client/ ./
RUN bun run build \
 && ls -la ../AspNetVue.Server/wwwroot

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS publish
WORKDIR /src
COPY AspNetVue.slnx ./
COPY src/AspNetVue.Server/ src/AspNetVue.Server/
COPY src/AspNetVue.Client/ src/AspNetVue.Client/
# wwwroot hasil stage client-build
COPY --from=client-build /src/src/AspNetVue.Server/wwwroot src/AspNetVue.Server/wwwroot/
RUN dotnet publish src/AspNetVue.Server/AspNetVue.Server.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "AspNetVue.Server.dll"]
