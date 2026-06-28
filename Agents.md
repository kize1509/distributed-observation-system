# SNUS Project Agent Instructions

## Source Of Truth

Implement exactly the requirements from `SNUS projekat - 2026.pdf`.
Do not add a web UI, dashboard, unrelated product features, or broad abstractions unless explicitly requested later.

## Required Scope

The system must use:

- ASP.NET Core
- Docker
- Kubernetes/Minikube YAML
- Docker Compose for local startup
- Entity Framework
- PostgreSQL
- HTTP/REST for client-server communication
- SignalR only for real-time alarm notifications from `NotificationService`
- encrypted and digitally signed sensor messages

The required components are:

- `IngestionService`
- `ConsensusService`
- `NotificationService`
- `Ingress`
- `ReportingService` for the `/api/reports` route and historical access
- console `SensorClient`

## Core Behavior Rules

- Maintain exactly 5 active sensors whenever at least 5 usable sensors exist.
- Store all received values on the server.
- Use alarm priority `0` for non-alarm values.
- Calculate consensus every minute from the previous minute only.
- Only `GOOD` quality data participates in consensus.
- Mark malicious sensors as `BAD`.
- Support temporary 30-second sensor blocking for tests.
- Treat a sensor as inactive if no message is received for 10 seconds.
- Support communication over a concrete network address, not only `localhost`.

## Implementation Order

Phase 1 builds infrastructure and minimal service implementations only:

- services compile and start
- endpoints and workers log placeholder activity
- PostgreSQL is wired
- Docker Compose exists and runs
- Kubernetes/Minikube YAML exists and validates
- startup docs are short and command-based

Phase 2 implements features using TDD. Every feature must follow:

1. Write a unit test.
2. Implement the feature.
3. Validate the feature shape against this file and the PDF.
4. Add an integration test.
5. Add a system test or runnable demo verification.

Before closing any feature, verify it matches the PDF and does not add extra behavior.
