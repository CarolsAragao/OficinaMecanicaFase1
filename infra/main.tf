terraform {
  required_version = ">= 1.4"
}

resource "terraform_data" "cluster" {
  input = "oficina-tf"

  provisioner "local-exec" {
    command = "kind create cluster --name ${self.input} --wait 180s"
  }

  provisioner "local-exec" {
    when    = destroy
    command = "kind delete cluster --name ${self.input}"
  }
}

resource "terraform_data" "banco" {
  triggers_replace = [
    terraform_data.cluster.id,
    filesha256("${path.module}/../k8s/01-configuracao.yaml"),
    filesha256("${path.module}/../k8s/02-volume-sql.yaml"),
    filesha256("${path.module}/../k8s/03-deployment-sql.yaml"),
    filesha256("${path.module}/../k8s/04-service-sql.yaml"),
  ]

  provisioner "local-exec" {
    command     = "kubectl --context kind-oficina-tf apply -f ../k8s/01-configuracao.yaml -f ../k8s/02-volume-sql.yaml -f ../k8s/03-deployment-sql.yaml -f ../k8s/04-service-sql.yaml"
    working_dir = path.module
  }
}

resource "terraform_data" "migracao" {
  depends_on = [terraform_data.banco]

  triggers_replace = [
    terraform_data.cluster.id,
    filesha256("${path.module}/../k8s/05-job-migracao.yaml"),
  ]

  provisioner "local-exec" {
    command     = "kubectl --context kind-oficina-tf rollout status deployment/sqlserver --timeout=300s && kubectl --context kind-oficina-tf apply -f ../k8s/05-job-migracao.yaml && kubectl --context kind-oficina-tf wait --for=condition=complete job/oficina-migracao --timeout=300s"
    working_dir = path.module
  }
}

resource "terraform_data" "api" {
  depends_on = [terraform_data.migracao]

  triggers_replace = [
    terraform_data.cluster.id,
    filesha256("${path.module}/../k8s/06-deployment-api.yaml"),
    filesha256("${path.module}/../k8s/07-service-api.yaml"),
  ]

  provisioner "local-exec" {
    command     = "kubectl --context kind-oficina-tf apply -f ../k8s/06-deployment-api.yaml -f ../k8s/07-service-api.yaml && kubectl --context kind-oficina-tf rollout status deployment/oficina-api --timeout=300s"
    working_dir = path.module
  }
}

resource "terraform_data" "escala_api" {
  depends_on = [terraform_data.api]

  triggers_replace = [
    terraform_data.cluster.id,
    filesha256("${path.module}/../k8s/08-hpa-api.yaml"),
  ]

  provisioner "local-exec" {
    command     = "kubectl --context kind-oficina-tf apply -f ../k8s/08-hpa-api.yaml"
    working_dir = path.module
  }
}
