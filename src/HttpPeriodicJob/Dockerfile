FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine as build
WORKDIR /src
COPY HttpPeriodicJob.csproj .
RUN dotnet restore .
COPY . .
RUN dotnet publish -o out -c Release -r linux-musl-x64

FROM mcr.microsoft.com/dotnet/runtime:5.0.7-alpine3.13
WORKDIR /app
COPY --from=build /src/out /app
CMD ["./HttpPeriodicJob"]