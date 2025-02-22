# Docker Monitoring on Windows
Monitor your Docker containers using prometheus, cAdvisor , node-exporter and grafana on Windows

Refer to this medium article for more details
https://medium.com/@mahesh.mahadevan/monitoring-docker-containers-on-windows-using-prometheus-grafana-c32bbb7ed04

## Pre-requisites

* docker on windows
* docker-compose

## Setup and Install

Clone this repo on your Windows OS machine where Docker is up and running.

Open Power shell window and run this command from current directory of this repository.

```docker-compose up```

Goto http://localhost:3000 for grafana dashboard (default user/pass : admin/admin )

## Configuration

By default, the following ports are exposed:

* Prometheus: 9090
* Grafana 3000
* cAdvisor: 8080

These ports can be customized via the environment variables (or .env file entries) `PROMETHEUS_PORT`, `GRAFANA_PORT`, and `CADVISOR_PORT`, respectively.
