FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY S3FileApi.csproj ./
RUN dotnet restore S3FileApi.csproj

COPY . ./
RUN dotnet publish S3FileApi.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish ./
USER 1000 

ENTRYPOINT ["dotnet", "S3FileApi.dll"]
