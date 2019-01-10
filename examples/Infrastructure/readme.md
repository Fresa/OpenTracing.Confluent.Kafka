# Running Kafka with Docker and Kubernetes
## Automatic process
Run ./docker-kafka-setup.ps1 in an elevated powershell session within this directory.
The script supports 'verbose' parameter if you whant more information to be printed.

Set the execution policy if that hasn't been done before

```sh
$ Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
```

```sh
$ .\docker-kafka-setup.ps1 -verbose
```
When the script is done you can create clusters and topics, se step 10 below
## Manual process
The same process can be done manually by executing the following steps in an elevated powershell command prompt within this directory.
1. Install Docker
https://www.docker.com/
2. Enable Kubernetes in Docker.
Settings -> Kubernetes -> Check "Enable Kubernetes" (also choose Kubernetes as Default orchestrator)
3. Install the Kubernetes Web UI
```sh
$ kubectl create -f https://raw.githubusercontent.com/kubernetes/dashboard/master/src/deploy/recommended/kubernetes-dashboard.yaml
$ kubectl proxy
``` 
- Kubernetes Web UI should now be accessable via http://localhost:8001/api/v1/namespaces/kube-system/services/kubernetes-dashboard/proxy
or http://127.0.0.1:8001/api/v1/namespaces/kube-system/services/https:kubernetes-dashboard:/proxy/
4. Install choco
https://chocolatey.org/docs/installation
5. Install Kompose
```sh
$ choco install kubernetes-kompose
```
6. Create the Kafka services and deployment in Kubernetes
```sh
$ kompose up
```
7. Port forward Kafka Manager from the Kubernetes Cluster to a local port. Find the exact name of the kafka-manager pod name in Kubernetes with:
```sh
$ kubectl get pods
```
Replace "kafka-manager-xxxxxxxxx-yyyyy" with the correct name and run:
```sh
$ kubectl port-forward kafka-manager-xxxxxxxxx-yyyyy 9000:9000
```
8. Port forward Kadrop from the Kubernetes Cluster to a local port. Replace "kafdrop-xxxxxxxxx-yyyyy" with the correct name and run:
```sh
$ kubectl port-forward kafdrop-xxxxxxxxx-yyyyy 9010:9010
```
You might wanna do this for the rest client and the server as well if you need to reach them from your host outside of the cluster.
The kafka server also needs a host redirect. You should add `127.0.0.1	kafka-server` to the hosts file found at `C:\Windows\System32\drivers\etc`.

9. Access Kafka Manager at
http://localhost:9000/

10. Add a Kafka Cluster by clicking Cluster -> Add Cluster
- Choose a name 
- Fill in the Cluster Zookeeper Hosts: zookeeper
- Check "Enable JMX Polling"
- Check "Poll consumer information"
- Check "Enable Logkafka"
11. Acces Kafdrop at
http://localhost:9010

After creating the cluster it should automatically have created a broker and two topics. You can now create and configure partions and more topics as you wish.

# Commands
Get all endpoints
```sh
$ kubectl get svc,ep -o wide
```