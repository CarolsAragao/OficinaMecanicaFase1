terraform {
  required_version = ">= 1.4"
}

resource "terraform_data" "cluster" {
  input = "oficina"

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
    filesha256("${path.module}/../k8s/02-banco.yaml"),
  ]

  provisioner "local-exec" {
    command     = "kubectl --context kind-oficina -n default apply -f ../k8s/01-configuracao.yaml -f ../k8s/02-banco.yaml"
    working_dir = path.module
  }
}

output "contexto_kubernetes" {
  value = "kind-${terraform_data.cluster.output}"
}
