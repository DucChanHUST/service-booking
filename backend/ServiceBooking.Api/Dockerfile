FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["ServiceBooking.Api.csproj", "./"]

RUN dotnet restore "ServiceBooking.Api.csproj"

COPY . .

RUN dotnet publish "ServiceBooking.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "ServiceBooking.Api.dll"]