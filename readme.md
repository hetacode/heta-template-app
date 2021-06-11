## Setup lightweight kubernetes in docker - [kind](https://kind.sigs.k8s.io/)
```console
curl -Lo ./kind https://kind.sigs.k8s.io/dl/v0.11.0/kind-linux-amd64
chmod +x ./kind
mv ./kind /some-dir-in-your-PATH/kind
```

And create local cluster:
```
kind create cluster
```