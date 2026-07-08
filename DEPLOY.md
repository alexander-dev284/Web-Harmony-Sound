# Despliegue en Render + Neon + Backblaze B2

Arquitectura: **2 servicios web** (API y MVC) en Render, **PostgreSQL** en Neon, **almacenamiento de archivos** en Backblaze B2 (compatible con S3).

> ⚠️ **Seguridad:** rota (regenera) todas las credenciales que estuvieron en el repo público
> (contraseña de app de Gmail, secret de PayPal). Nunca las pongas en `appsettings.json`; van
> en variables de entorno de Render o en `appsettings.Development.json` (local, ya ignorado por git).

---

## 1. Base de datos — Neon
1. Crea un proyecto en https://neon.tech (gratis, sin tarjeta).
2. Copia la connection string en **formato .NET / Npgsql**, del estilo:
   ```
   Host=ep-xxxx.us-east-2.aws.neon.tech;Database=neondb;Username=xxxx;Password=xxxx;SSL Mode=Require;Trust Server Certificate=true
   ```

## 2. Almacenamiento — Backblaze B2
1. Crea cuenta en https://backblaze.com y activa **B2 Cloud Storage**.
2. Crea un **Bucket** público (ej. `unisound-media`).
3. En **App Keys**, crea una llave: obtienes `keyID` y `applicationKey` (cópialos, el segundo se muestra una sola vez).
4. Anota el **endpoint S3** y la **región** del bucket (ej. `s3.us-east-005.backblazeb2.com`, región `us-east-005`).

## 3. Migraciones (crear las tablas en Neon)
Desde tu máquina, apuntando temporalmente a Neon:
```bash
dotnet ef database update --project HarmonySound.API \
  --connection "Host=...;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true"
```
(o pon la connection string de Neon en `appsettings.Development.json` y corre `dotnet ef database update`).

## 4. Servicios en Render (https://render.com)
Crea **dos Web Services** desde el repo de GitHub, tipo **Docker**:

### Servicio API
- **Dockerfile Path:** `HarmonySound.API/Dockerfile`
- **Docker Build Context Directory:** `.` (raíz del repo)
- **Variables de entorno:**
  | Variable | Valor |
  |---|---|
  | `ASPNETCORE_ENVIRONMENT` | `Production` |
  | `ConnectionStrings__HarmonySoundDbContext` | *(connection string de Neon)* |
  | `Jwt__Key` | *(una clave larga y aleatoria)* |
  | `ApiUrl` | `https://<tu-api>.onrender.com` |
  | `AppUrl` | `https://<tu-mvc>.onrender.com` |
  | `Storage__ServiceUrl` | `https://s3.us-east-005.backblazeb2.com` |
  | `Storage__Region` | `us-east-005` |
  | `Storage__AccessKey` | *(keyID de B2)* |
  | `Storage__SecretKey` | *(applicationKey de B2)* |
  | `Storage__Bucket` | `unisound-media` |
  | `Storage__PublicBaseUrl` | `https://s3.us-east-005.backblazeb2.com/unisound-media` |
  | `SmtpSettings__FromEmail` | *(correo remitente, ej. tu Gmail verificado en Brevo)* |
  | `SmtpSettings__FromName` | `UniSound` |
  | `Brevo__ApiKey` | *(API key de Brevo — el correo se envía por su API HTTP, no por SMTP)* |
  | `PayPal__ClientId` | *(client id)* |
  | `PayPal__ClientSecret` | *(client secret)* |
  | `PayPal__ReturnUrl` | `https://<tu-mvc>.onrender.com/Plans/PaymentSuccess` |
  | `PayPal__CancelUrl` | `https://<tu-mvc>.onrender.com/Plans/PaymentCancel` |

### Servicio MVC
- **Dockerfile Path:** `HarmonySound.MVC/Dockerfile`
- **Docker Build Context Directory:** `.` (raíz del repo)
- **Variables de entorno:**
  | Variable | Valor |
  |---|---|
  | `ASPNETCORE_ENVIRONMENT` | `Production` |
  | `ApiUrl` | `https://<tu-api>.onrender.com` |

## 5. Orden recomendado (por el "huevo y la gallina" de las URLs)
1. Despliega la **API** → obtienes su URL.
2. Despliega la **MVC** con `ApiUrl` = URL de la API → obtienes la URL de la MVC.
3. Vuelve a la **API** y completa `AppUrl` y las `PayPal__*Url` con la URL de la MVC → re-despliega.

---

## Notas
- Los servicios gratuitos de Render **duermen** tras ~15 min de inactividad (primera carga lenta).
- El almacenamiento de archivos es compatible con S3, así que si algún día cambias a Cloudflare R2 o
  Supabase, solo cambias las variables `Storage__*` (el código no se toca).
- Variables anidadas: en .NET, `Seccion__Clave` (doble guion bajo) equivale a `"Seccion": { "Clave": ... }`.
