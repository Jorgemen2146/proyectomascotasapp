# Guía de Despliegue de DogPlatform en AWS (Amazon ECS + Fargate)

> ⚠️ **ACCIÓN URGENTE**: Se detectaron y removieron credenciales de base de datos y un JWT secret
> committeados en texto plano en varios `appsettings.json` (Pets, Matching, Identity, Genealogy),
> apuntando a una instancia RDS real (`bdprueba.cne8eagyg4ej.us-east-2.rds.amazonaws.com`).
> **Debes rotar esa contraseña de RDS y el JWT secret inmediatamente**, ya que quedaron expuestos
> en el historial de git. Usa `git log -p -- <archivo>` para confirmar el alcance y considera
> reescribir el historial (`git filter-repo` / BFG) si el repo es público o tiene colaboradores externos.

## 1. Servicio de cómputo recomendado: Amazon ECS con AWS Fargate

Con 8 microservicios independientes (ApiGateway + 7 dominios), **ECS + Fargate** es la opción
recomendada porque:
- No gestionas servidores EC2 (Fargate es serverless para contenedores).
- Cada microservicio se define como un **ECS Service** con su propia **Task Definition**,
  escalando de forma independiente.
- Se integra de forma nativa con:
  - **Amazon ECR** — registro de imágenes Docker.
  - **Application Load Balancer (ALB)** — expone el `ApiGateway` públicamente.
  - **AWS Cloud Map (Service Connect)** — descubrimiento de servicios interno (reemplaza los
	hosts `localhost:<puerto>` de `ocelot.json` por DNS internos tipo `pets-api.dogplatform.local`).
  - **Amazon RDS for SQL Server** — base de datos ya usada (confirmado por el proyecto).
  - **AWS Secrets Manager** — connection strings y JWT secret, inyectados como variables de entorno.
  - **Amazon CloudWatch Logs** — logging centralizado.

Alternativas si más adelante cambian los requisitos:
- **Amazon EKS**: si se necesita Kubernetes real (portabilidad multi-nube, Helm charts, etc.).
- **AWS App Runner**: despliegue más simple aún, pero menos control de red/descubrimiento interno.

## 2. Cambios ya aplicados en este repositorio

- `Dockerfile` multi-stage (SDK 9.0 → aspnet 9.0) en cada servicio API y en el ApiGateway.
- `.dockerignore` en la raíz.
- `docker-compose.yml` para probar todo el stack localmente antes de subir a AWS.
- Credenciales sensibles removidas de `appsettings.json` (ahora deben proveerse vía variables
  de entorno / Secrets Manager, nunca committeadas).

## 3. Pasos para desplegar en ECS Fargate

### 3.1 Crear repositorios ECR (uno por servicio)
```powershell
aws ecr create-repository --repository-name dogplatform/apigateway
aws ecr create-repository --repository-name dogplatform/identity-api
aws ecr create-repository --repository-name dogplatform/pets-api
aws ecr create-repository --repository-name dogplatform/genealogy-api
aws ecr create-repository --repository-name dogplatform/health-api
aws ecr create-repository --repository-name dogplatform/matching-api
aws ecr create-repository --repository-name dogplatform/notification-api
aws ecr create-repository --repository-name dogplatform/veterinarian-api
aws ecr create-repository --repository-name dogplatform/walks-api
```

### 3.2 Construir y subir las imágenes
Desde la raíz del repositorio (el `context: .` de cada Dockerfile requiere esto):
```powershell
aws ecr get-login-password --region us-east-2 | docker login --username AWS --password-stdin <account-id>.dkr.ecr.us-east-2.amazonaws.com

docker build -t dogplatform/pets-api -f src/Services/Pets/DogPlatform.Pets.API/Dockerfile .
docker tag dogplatform/pets-api:latest <account-id>.dkr.ecr.us-east-2.amazonaws.com/dogplatform/pets-api:latest
docker push <account-id>.dkr.ecr.us-east-2.amazonaws.com/dogplatform/pets-api:latest
# repetir para cada servicio
```

### 3.3 Base de datos — Amazon RDS for SQL Server
- Usa la instancia RDS existente o crea una nueva (`db.t3.small` para empezar).
- Crea una base de datos por servicio (`DogPlatform_PetsDb`, `DogPlatform_IdentityDb`, etc.) —
  patrón "database per service" ya usado en el proyecto.
- Guarda las credenciales en **AWS Secrets Manager**, NO en appsettings.json.

### 3.4 Secretos — AWS Secrets Manager / Parameter Store
Por cada servicio, crea un secreto con la connection string y, en Identity, el JWT secret:
```powershell
aws secretsmanager create-secret --name dogplatform/pets-api/connectionstring --secret-string "Server=<rds-endpoint>,1433;Database=DogPlatform_PetsDb;User Id=dogplatform_app;Password=<NUEVA-PASSWORD>;Encrypt=True;TrustServerCertificate=True;"
aws secretsmanager create-secret --name dogplatform/jwt-secret --secret-string "<nuevo-jwt-secret-generado>"
```
En la Task Definition de ECS, referencia el secreto en `secrets` (no en `environment`):
```json
"secrets": [
  { "name": "ConnectionStrings__PetsDb", "valueFrom": "arn:aws:secretsmanager:...:secret:dogplatform/pets-api/connectionstring" }
]
```

### 3.5 Red — VPC, Service Connect / Cloud Map y ALB
1. Crea (o reutiliza) una VPC con subredes privadas (para los servicios) y públicas (para el ALB).
2. Crea un **ECS Cluster** con Fargate.
3. Habilita **Service Connect** (o Cloud Map) en el cluster — esto da a cada servicio un nombre
   DNS interno, ej. `pets-api.dogplatform`, `identity-api.dogplatform`.
4. Actualiza `ocelot.json` (o crea `ocelot.Production.json`, cargado según `ASPNETCORE_ENVIRONMENT`)
   para que `DownstreamHostAndPorts` apunte a esos nombres DNS en vez de `localhost`:
   ```json
   "DownstreamHostAndPorts": [ { "Host": "pets-api.dogplatform", "Port": 8080 } ]
   ```
5. Crea un **Application Load Balancer** público apuntando únicamente al servicio `apigateway`
   (los demás servicios permanecen internos, sin exposición directa a internet).

### 3.6 Task Definitions y Services de ECS
Por cada imagen: 1 Task Definition (CPU/memoria según carga, ej. 0.25 vCPU / 512 MB para empezar)
+ 1 ECS Service (con Service Connect habilitado y, solo para `apigateway`, target group del ALB).

### 3.7 Logging y observabilidad
- Configura el log driver `awslogs` en cada Task Definition → CloudWatch Logs.
- `DogPlatform.Logging` (BuildingBlock existente) puede configurarse para escribir a consola,
  que ECS/Fargate reenvía automáticamente a CloudWatch.

## 4. Checklist antes de ir a producción
- [ ] Rotar la password de RDS y el JWT secret expuestos (ver aviso arriba).
- [ ] Ningún `appsettings.json` debe contener secretos — usar Secrets Manager o variables de entorno.
- [ ] `ocelot.json` debe apuntar a nombres DNS de Service Connect/Cloud Map, no a `localhost`.
- [ ] Probar el stack completo con `docker compose up --build` localmente antes de publicar.
- [ ] Configurar HTTPS en el ALB (certificado de AWS Certificate Manager).
- [ ] Definir Auto Scaling por servicio según CPU/memoria.
- [ ] (Opcional) Añadir un pipeline CI/CD (GitHub Actions → ECR → ECS) para despliegues automáticos.
