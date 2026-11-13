$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

kubectl --context kind-oficina apply -f https://github.com/kubernetes-sigs/metrics-server/releases/download/v0.8.1/components.yaml

# Somente para o kind local, cujos kubelets usam certificados autoassinados.
$ajuste = '[{"op":"add","path":"/spec/template/spec/containers/0/args/-","value":"--kubelet-insecure-tls"}]'
$deployment = kubectl --context kind-oficina -n kube-system get deployment metrics-server -o json | ConvertFrom-Json
if ($deployment.spec.template.spec.containers[0].args -notcontains '--kubelet-insecure-tls') {
    kubectl --context kind-oficina -n kube-system patch deployment metrics-server --type=json --patch $ajuste
}
kubectl --context kind-oficina -n kube-system rollout status deployment/metrics-server --timeout=180s
