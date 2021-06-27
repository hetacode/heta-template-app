#!/bin/bash

REDPANDA_VERSION=v21.6.5

kind create cluster --name hads
kubectl create ns hads-app

# Install Cert Manager
helm repo add jetstack https://charts.jetstack.io && \
helm repo update && \
helm install \
  cert-manager jetstack/cert-manager \
  --namespace cert-manager \
  --create-namespace \
  --version v1.2.0 \
  --wait \
  --set installCRDs=true

# Wait for ready cert-manager-webhook pod
# while [ "$(kubectl get pods -l=app='webhook' -o jsonpath='{.items[*].status.containerStatuses[0].ready}' -n cert-manager)" != "true" ]; do
#    sleep 5
#    echo "Waiting for Pod to be ready."
# done

# Redpanda
# Install cluster operator
kubectl apply \
  -k https://github.com/vectorizedio/redpanda/src/go/k8s/config/crd\?ref\=$REDPANDA_VERSION

helm repo add redpanda https://charts.vectorized.io/ && \
helm repo update
helm install \
  --namespace redpanda-system \
  --create-namespace redpanda-system \
  --version $REDPANDA_VERSION \
  --wait \
  redpanda/redpanda-operator

kubectl apply \
  -n hads-app \
  --wait \
  -f k8s_configs/redpanda_one_node_cluster.yaml \
  -f k8s_configs/redpanda_service.yaml

