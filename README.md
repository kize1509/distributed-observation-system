# Distributed Observation System


## Docker Compose Startup

1. Build and start everything:
   ```bash
   docker compose up --build
   ```
2. Check the ingress health endpoint:
   ```bash
   curl http://localhost:8080/healthz
   ```
3. Stop the system:
   ```bash
   docker compose down
   ```

## Two-Machine Run

The backend runs on one machine and the sensor clients on another, communicating over
a concrete network address (not `localhost`), as required by the project.

1. On **Machine A (server)**, find its LAN IP (e.g. `192.168.1.50`) and start the backend:
   ```bash
   docker compose -f docker-compose.server.yml up --build
   ```
   Only Ingress (`8080`) is exposed on the network; Postgres is bound to `127.0.0.1`.
   Ensure the host firewall allows inbound TCP `8080`.

2. On **Machine B (clients)**, point the sensors at Machine A and start them:
   ```bash
   SERVER_URL=http://192.168.1.50:8080 docker compose -f docker-compose.clients.yml up --build
   ```

3. Verify from Machine B that the server is reachable:
   ```bash
   curl http://192.168.1.50:8080/healthz
   ```

## Real-Time Alarm Notifications

`NotificationService` records every alarm in the `AlarmEvents` table and broadcasts it live to
subscribers over a SignalR hub at `/hubs/alarms`, proxied through Ingress.

Ingress is a plain HTTP reverse proxy (it forwards ordinary request/response calls) and does not
forward a WebSocket upgrade handshake. To keep the hub working end-to-end through Ingress without
rewriting the proxy, both the hub and its clients are pinned to the **Long Polling** transport
instead of WebSockets. This is slightly less efficient than a persistent WebSocket connection, but
delivers alarms with sub-second latency, which is real-time enough for this system, and requires
no special proxy support beyond plain HTTP.

A minimal console demo client, `AlarmMonitor`, is included to observe alarms live during a
defense/demo:

```bash
make monitor                                    # same machine, defaults to http://localhost:8080
make monitor API_URL=http://192.168.1.50:8080   # remote server (two-machine scenario)
```

It prints each alarm colored by priority (yellow/orange/red for priority 1/2/3), matching the
console output already used by `SensorClient` and `IngestionService`.

## Historical Reports API

`ReportingService` exposes read-only historical queries backed by PostgreSQL, reachable through
Ingress under `/api/reports`. All list endpoints accept an optional `take` (default 200, max 1000)
and, where applicable, `sensorId`, `from` and `to` (ISO-8601) query parameters.

```bash
# Historical sensor readings (optionally filtered)
curl "http://localhost:8080/api/reports/readings?sensorId=sensor-1&take=50"

# Recorded alarm events
curl "http://localhost:8080/api/reports/alarms?from=2026-07-07T00:00:00Z"

# Per-minute consensus values
curl "http://localhost:8080/api/reports/consensus?take=10"

# Current status of every sensor that has ever been part of the system
curl "http://localhost:8080/api/reports/sensors"
```

## Local Build

1. Restore and build:
   ```bash
   DOTNET_CLI_HOME=/tmp/codex-dotnet-home dotnet build DistributedObservationSystem.slnx
   ```
2. Run one service locally if needed:
   ```bash
   DOTNET_CLI_HOME=/tmp/codex-dotnet-home dotnet run --project src/IngestionService/IngestionService.csproj
   ```

## Minikube Startup

1. Start Minikube and point Docker at it:
   ```bash
   minikube start
   eval $(minikube docker-env)
   ```
2. Build and load images for Minikube:
   ```bash
   docker build -f src/IngestionService/Dockerfile -t snus-ingestion:local .
   docker build -f src/ConsensusService/Dockerfile -t snus-consensus:local .
   docker build -f src/NotificationService/Dockerfile -t snus-notification:local .
   docker build -f src/ReportingService/Dockerfile -t snus-reporting:local .
   docker build -f src/Ingress/Dockerfile -t snus-ingress:local .
   docker build -f src/SensorClient/Dockerfile -t snus-sensor-client:local .
   minikube image load snus-ingestion:local snus-consensus:local snus-notification:local snus-reporting:local snus-ingress:local snus-sensor-client:local
   ```
3. Apply manifests:
   ```bash
   kubectl apply -f k8s/
   ```

