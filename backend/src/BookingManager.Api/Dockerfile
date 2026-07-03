FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/BookingManager.Api/BookingManager.Api.csproj src/BookingManager.Api/
RUN dotnet restore src/BookingManager.Api/BookingManager.Api.csproj

COPY src/BookingManager.Api/ src/BookingManager.Api/
WORKDIR /src/src/BookingManager.Api
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "BookingManager.Api.dll"]
