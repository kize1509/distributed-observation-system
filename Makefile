.PHONY: up down up-server up-clients up-minikube-server up-minikube-sensors down-minikube dashboard redeploy db-forward db-clear block-sensor minikube-url monitor

SERVER_URL ?=
API_URL    ?= http://localhost:8080
SENSOR_ID  ?= sensor-1

# ─── Docker Compose (local dev) ───────────────────────────────────────────────
# All-in-one: server + sensors on one machine via docker compose.
up:
	docker compose up --build

down:
	docker compose down

# Server only (no sensors). Pair with up-clients from the same or another machine.
up-server:
	docker compose -f docker-compose.server.yml up --build

# Sensors only. SERVER_URL must point at the running server.
#   Same machine:      make up-clients SERVER_URL=http://localhost:8080
#   Remote server:     make up-clients SERVER_URL=http://<server-ip>:8080
up-clients:
	SERVER_URL=$(SERVER_URL) docker compose -f docker-compose.clients.yml up --build

# ─── Minikube ─────────────────────────────────────────────────────────────────
# Scenario A — all on one machine:
#   make up-minikube-server
#   make up-minikube-sensors             # sensors use internal DNS http://ingress:8080
#
# Scenario B — split machines (server on Machine A, sensors on Machine B):
#   Machine A:  make up-minikube-server
#               make minikube-url          # prints the SERVER_URL to use on Machine B
#   Machine B:  make up-minikube-sensors SERVER_URL=http://<machine-a-ip>:30080

# Deploys the full server stack into Minikube (no sensors).
up-minikube-server:
	minikube start
	eval $$(minikube docker-env) && \
	docker build -f src/Migrator/Dockerfile            -t snus-migrator:local        . && \
	docker build -f src/IngestionService/Dockerfile    -t snus-ingestion:local       . && \
	docker build -f src/ConsensusService/Dockerfile    -t snus-consensus:local       . && \
	docker build -f src/NotificationService/Dockerfile -t snus-notification:local    . && \
	docker build -f src/ReportingService/Dockerfile    -t snus-reporting:local       . && \
	docker build -f src/Ingress/Dockerfile             -t snus-ingress:local         .
	kubectl apply -f k8s/00-namespace.yaml -f k8s/01-config.yaml -f k8s/postgres.yaml -f k8s/migrator.yaml
	kubectl wait --for=condition=complete job/migrator -n snus --timeout=120s
	kubectl apply -f k8s/services.yaml -f k8s/ingress.yaml

# Deploys sensors as k8s pods into Minikube.
# Same machine: uses internal DNS http://ingress:8080 (NodePort hairpin NAT doesn't work in Minikube).
# Different machine (Machine B): pass SERVER_URL=http://<machine-a-ip>:30080 to override.
up-minikube-sensors:
	minikube start
	eval $$(minikube docker-env) && \
	docker build -f src/SensorClient/Dockerfile -t snus-sensor-client:local .
	SERVER_URL=$${SERVER_URL:-http://ingress:8080} envsubst < k8s/sensors.yaml | kubectl apply -f -

# Runs sensors via docker compose on this machine, pointing at a local Minikube server.
# Prints the SERVER_URL to use when targeting this machine's Minikube from elsewhere.
minikube-url:
	@echo http://$$(minikube ip):30080

# ─── Minikube utilities ───────────────────────────────────────────────────────
down-minikube:
	kubectl delete -f k8s/
	minikube stop

# Rebuilds all images and restarts all deployments in the running cluster.
redeploy:
	eval $$(minikube docker-env) && \
	docker build -f src/IngestionService/Dockerfile    -t snus-ingestion:local       . && \
	docker build -f src/ConsensusService/Dockerfile    -t snus-consensus:local       . && \
	docker build -f src/NotificationService/Dockerfile -t snus-notification:local    . && \
	docker build -f src/ReportingService/Dockerfile    -t snus-reporting:local       . && \
	docker build -f src/Ingress/Dockerfile             -t snus-ingress:local         . && \
	docker build -f src/SensorClient/Dockerfile        -t snus-sensor-client:local   .
	kubectl rollout restart deployment -n snus

db-forward:
	kubectl port-forward svc/postgres 5432:5432 -n snus

block-sensor:
	curl -s -X POST $(API_URL)/api/ingest/sensors/$(SENSOR_ID)/block | cat

db-clear:
	kubectl exec -n snus deployment/postgres -- psql -U observation -d observation \
		-c "TRUNCATE \"SensorReadings\", \"ConsensusReadings\", \"AlarmEvents\", \"Sensors\" RESTART IDENTITY CASCADE;"

dashboard:
	minikube dashboard

# Runs the AlarmMonitor demo client locally, printing real-time alarms received
# from NotificationService's SignalR hub through Ingress.
#   Same machine:  make monitor
#   Remote server: make monitor API_URL=http://<server-ip>:8080
monitor:
	SERVER_URL=$(API_URL) DOTNET_CLI_HOME=/tmp/codex-dotnet-home dotnet run --project src/AlarmMonitor/AlarmMonitor.csproj
