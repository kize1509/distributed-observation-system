.PHONY: up down up-server up-clients up-minikube down-minikube dashboard redeploy db-forward db-clear block-sensor

SERVER_URL ?= http://localhost:8080
API_URL    ?= http://localhost:8080
SENSOR_ID  ?= sensor-1

up:
	docker compose up --build

down:
	docker compose down

up-server:
	docker compose -f docker-compose.server.yml up --build

up-clients:
	SERVER_URL=$(SERVER_URL) docker compose -f docker-compose.clients.yml up --build

up-minikube:
	minikube start
	eval $$(minikube docker-env) && \
	docker build -f src/Migrator/Dockerfile         -t snus-migrator:local        . && \
	docker build -f src/IngestionService/Dockerfile  -t snus-ingestion:local       . && \
	docker build -f src/ConsensusService/Dockerfile  -t snus-consensus:local       . && \
	docker build -f src/NotificationService/Dockerfile -t snus-notification:local  . && \
	docker build -f src/ReportingService/Dockerfile  -t snus-reporting:local       . && \
	docker build -f src/Ingress/Dockerfile           -t snus-ingress:local         . && \
	docker build -f src/SensorClient/Dockerfile      -t snus-sensor-client:local   .
	kubectl apply -f k8s/00-namespace.yaml -f k8s/01-config.yaml -f k8s/postgres.yaml -f k8s/migrator.yaml
	kubectl wait --for=condition=complete job/migrator -n snus --timeout=120s
	kubectl apply -f k8s/services.yaml -f k8s/ingress.yaml -f k8s/sensors.yaml

down-minikube:
	kubectl delete -f k8s/
	minikube stop

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
