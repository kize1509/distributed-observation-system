 FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
RUN dotnet tool install --global dotnet-ef --version 10.0.8
ENV PATH="$PATH:/root/.dotnet/tools"
COPY . .
RUN dotnet restore src/DistributedObservationSystem.Persistence/DistributedObservationSystem.Persistence.csproj
RUN dotnet ef migrations bundle \
    --project src/DistributedObservationSystem.Persistence/DistributedObservationSystem.Persistence.csproj \
    -o /app/efbundle --force

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/efbundle ./efbundle
ENTRYPOINT ["/bin/sh", "-c", "./efbundle --connection \"$ConnectionStrings__ObservationDatabase\""]
