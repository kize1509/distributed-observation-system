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

