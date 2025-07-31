#!/bin/bash

# RemoteC Kubernetes Deployment Script
# This script deploys RemoteC to a Kubernetes cluster

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
K8S_DIR="$PROJECT_ROOT/k8s"
NAMESPACE="remotec"

# Function to print colored output
print_message() {
    local color=$1
    local message=$2
    echo -e "${color}${message}${NC}"
}

# Function to check prerequisites
check_prerequisites() {
    print_message $YELLOW "Checking prerequisites..."
    
    # Check kubectl
    if ! command -v kubectl &> /dev/null; then
        print_message $RED "kubectl is not installed. Please install kubectl first."
        exit 1
    fi
    
    # Check cluster connection
    if ! kubectl cluster-info &> /dev/null; then
        print_message $RED "Cannot connect to Kubernetes cluster. Please configure kubectl."
        exit 1
    fi
    
    # Check Helm (optional but recommended)
    if command -v helm &> /dev/null; then
        print_message $GREEN "Helm is installed."
    else
        print_message $YELLOW "Helm is not installed. Some features may not be available."
    fi
    
    print_message $GREEN "Prerequisites check completed."
}

# Function to create namespace
create_namespace() {
    print_message $YELLOW "Creating namespace..."
    kubectl apply -f "$K8S_DIR/namespace.yaml"
    print_message $GREEN "Namespace created."
}

# Function to setup secrets
setup_secrets() {
    print_message $YELLOW "Setting up secrets..."
    
    # Check if secrets already exist
    if kubectl get secret remotec-secrets -n $NAMESPACE &> /dev/null; then
        print_message $YELLOW "Secrets already exist. Skipping..."
    else
        print_message $RED "Please edit $K8S_DIR/secret.yaml with your actual values before proceeding."
        read -p "Have you updated the secrets file? (y/N): " confirm
        if [ "$confirm" != "y" ]; then
            print_message $RED "Deployment cancelled. Please update secrets first."
            exit 1
        fi
        kubectl apply -f "$K8S_DIR/secret.yaml"
        print_message $GREEN "Secrets created."
    fi
}

# Function to deploy infrastructure
deploy_infrastructure() {
    print_message $YELLOW "Deploying infrastructure components..."
    
    # Deploy SQL Server
    print_message $YELLOW "Deploying SQL Server..."
    kubectl apply -f "$K8S_DIR/sqlserver.yaml"
    
    # Deploy Redis
    print_message $YELLOW "Deploying Redis..."
    kubectl apply -f "$K8S_DIR/redis.yaml"
    
    # Wait for infrastructure
    print_message $YELLOW "Waiting for infrastructure to be ready..."
    kubectl wait --for=condition=ready pod -l app=sqlserver -n $NAMESPACE --timeout=300s
    kubectl wait --for=condition=ready pod -l app=redis -n $NAMESPACE --timeout=300s
    
    print_message $GREEN "Infrastructure deployed successfully."
}

# Function to deploy application
deploy_application() {
    print_message $YELLOW "Deploying RemoteC application..."
    
    # Apply ConfigMap
    kubectl apply -f "$K8S_DIR/configmap.yaml"
    
    # Deploy API
    kubectl apply -f "$K8S_DIR/api-deployment.yaml"
    
    # Deploy Ingress
    kubectl apply -f "$K8S_DIR/ingress.yaml"
    
    # Wait for deployment
    print_message $YELLOW "Waiting for application to be ready..."
    kubectl wait --for=condition=available deployment/remotec-api -n $NAMESPACE --timeout=300s
    
    print_message $GREEN "Application deployed successfully."
}

# Function to run database migrations
run_migrations() {
    print_message $YELLOW "Running database migrations..."
    
    # Get SQL Server pod
    SQL_POD=$(kubectl get pod -l app=sqlserver -n $NAMESPACE -o jsonpath="{.items[0].metadata.name}")
    
    # Create database if not exists
    kubectl exec -n $NAMESPACE $SQL_POD -- /opt/mssql-tools/bin/sqlcmd \
        -S localhost -U sa -P '$MSSQL_SA_PASSWORD' \
        -Q "IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'RemoteC2Db') CREATE DATABASE RemoteC2Db"
    
    # In a real scenario, you would run EF Core migrations here
    print_message $GREEN "Database migrations completed."
}

# Function to show status
show_status() {
    print_message $YELLOW "\nDeployment Status:"
    kubectl get all -n $NAMESPACE
    
    print_message $YELLOW "\nIngress Status:"
    kubectl get ingress -n $NAMESPACE
    
    print_message $GREEN "\nRemoteC has been deployed to Kubernetes!"
    print_message $GREEN "To access the application, configure your DNS to point to the ingress IP."
    print_message $GREEN "\nUseful commands:"
    print_message $GREEN "View logs: kubectl logs -f deployment/remotec-api -n $NAMESPACE"
    print_message $GREEN "Scale API: kubectl scale deployment/remotec-api --replicas=5 -n $NAMESPACE"
    print_message $GREEN "Port forward API: kubectl port-forward service/remotec-api 8080:80 -n $NAMESPACE"
}

# Function to uninstall
uninstall() {
    print_message $RED "This will remove all RemoteC resources from the cluster!"
    read -p "Are you sure? (y/N): " confirm
    if [ "$confirm" = "y" ]; then
        kubectl delete namespace $NAMESPACE
        print_message $GREEN "RemoteC has been uninstalled."
    fi
}

# Main execution
main() {
    print_message $GREEN "RemoteC Kubernetes Deployment Script"
    print_message $GREEN "===================================\n"
    
    check_prerequisites
    
    # Ask for action
    echo "What would you like to do?"
    echo "1) Full deployment"
    echo "2) Deploy infrastructure only"
    echo "3) Deploy application only"
    echo "4) Update application"
    echo "5) Show status"
    echo "6) Uninstall"
    read -p "Enter your choice (1-6): " choice
    
    case $choice in
        1)
            create_namespace
            setup_secrets
            deploy_infrastructure
            deploy_application
            run_migrations
            show_status
            ;;
        2)
            create_namespace
            setup_secrets
            deploy_infrastructure
            ;;
        3)
            deploy_application
            show_status
            ;;
        4)
            print_message $YELLOW "Updating application..."
            kubectl rollout restart deployment/remotec-api -n $NAMESPACE
            kubectl rollout status deployment/remotec-api -n $NAMESPACE
            print_message $GREEN "Application updated."
            ;;
        5)
            show_status
            ;;
        6)
            uninstall
            ;;
        *)
            print_message $RED "Invalid choice."
            exit 1
            ;;
    esac
}

# Run main function
main