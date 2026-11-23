FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["FlightBooking.sln", "./"]
COPY ["src/FlightBooking.Api/FlightBooking.Api.csproj", "src/FlightBooking.Api/"]
COPY ["src/FlightBooking.Shared/FlightBooking.Shared.csproj", "src/FlightBooking.Shared/"]
COPY ["src/FlightBooking.Modules.Identity/FlightBooking.Modules.Identity.csproj", "src/FlightBooking.Modules.Identity/"]
COPY ["src/FlightBooking.Modules.Flights/FlightBooking.Modules.Flights.csproj", "src/FlightBooking.Modules.Flights/"]
COPY ["src/FlightBooking.Modules.Bookings/FlightBooking.Modules.Bookings.csproj", "src/FlightBooking.Modules.Bookings/"]
COPY ["src/FlightBooking.Modules.Payments/FlightBooking.Modules.Payments.csproj", "src/FlightBooking.Modules.Payments/"]
COPY ["src/FlightBooking.Modules.Reporting/FlightBooking.Modules.Reporting.csproj", "src/FlightBooking.Modules.Reporting/"]

RUN dotnet restore "src/FlightBooking.Api/FlightBooking.Api.csproj"

COPY . .
RUN dotnet publish "src/FlightBooking.Api/FlightBooking.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

USER app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FlightBooking.Api.dll"]