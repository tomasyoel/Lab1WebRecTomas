[![Review Assignment Due Date](https://classroom.github.com/assets/deadline-readme-button-22041afd0340ce965d47ae6ef1cefeee28c7c493a6346c4f15d667ab976d596c.svg)](https://classroom.github.com/a/Wvi4sqrO)
[![Open in Codespaces](https://classroom.github.com/assets/launch-codespace-2972f46106e565e64193e422d61a12cf1da4916b45550586e14ef0a7c637dd04.svg)](https://classroom.github.com/open-in-codespaces?assignment_repo_id=17697170)
# SESION DE LABORATORIO N° 01: Construyendo una Aplicación Web con ASP.NET MVC

## NOMBRE: 

## OBJETIVOS
  * Comprender el desarrollo una Aplicación Web utilizando ASP.NET MVC

## REQUERIMIENTOS
  * Conocimientos: 
    - Conocimientos básicos de SQL.
    - Conocimientos shell y comandos en modo terminal.
  * Hardware:
    - Virtualization activada en el BIOS.
    - CPU SLAT-capable feature.
    - Al menos 4GB de RAM.
  * Software:
    - Windows 10 64bit: Pro, Enterprise o Education (1607 Anniversary Update, Build 14393 o Superior)
    - Docker Desktop 
    - Powershell versión 7.x
    - .Net 8
    - Azure CLI

## CONSIDERACIONES INICIALES
  * Tener una cuenta en Infracost (https://www.infracost.io/), sino utilizar su cuenta de github para generar su cuenta y generar un token.
  * Tener una cuenta en SonarCloud (https://sonarcloud.io/), sino utilizar su cuenta de github para generar su cuenta y generar un token. El token debera estar registrado en su repositorio de Github con el nombre de SONAR_TOKEN. 
  * Tener una cuenta con suscripción en Azure (https://portal.azure.com/). Tener el ID de la Suscripción, que se utilizará en el laboratorio
  * Clonar el repositorio mediante git para tener los recursos necesarios en una ubicación que no sea restringida del sistema.

## DESARROLLO

### PREPARACION DE LA INFRAESTRUCTURA

1. Iniciar la aplicación Powershell o Windows Terminal en modo administrador, ubicarse en ua ruta donde se ha realizado la clonación del repositorio
```Powershell
md Infra
```
2. Abrir Visual Studio Code, seguidamente abrir la carpeta del repositorio clonado del laboratorio, en el folder Infra, crear el archivo main.tf con el siguiente contenido
```Terraform
# Configure the Azure provider
terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0.0"
    }
  }
  required_version = ">= 0.14.9"
}
provider "azurerm" {
  features {}
  subscription_id = "ID SUSCRIPCION AZURE"
}

# Generate a random integer to create a globally unique name
resource "random_integer" "ri" {
  min = 100
  max = 999
}

# Create the resource group
resource "azurerm_resource_group" "rg" {
  name     = "upt-arg-${random_integer.ri.result}"
  location = "eastus"
}

# Create the Linux App Service Plan
resource "azurerm_service_plan" "appserviceplan" {
  name                = "upt-asp-${random_integer.ri.result}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  os_type             = "Linux"
  sku_name            = "F1"
}

# Create the web app, pass in the App Service Plan ID
resource "azurerm_linux_web_app" "webapp" {
  name                  = "upt-awa-${random_integer.ri.result}"
  location              = azurerm_resource_group.rg.location
  resource_group_name   = azurerm_resource_group.rg.name
  service_plan_id       = azurerm_service_plan.appserviceplan.id
  depends_on            = [azurerm_service_plan.appserviceplan]
  //https_only            = true
  site_config {
    minimum_tls_version = "1.2"
    always_on = false
    application_stack {
      dotnet_version = "8.0"
    }
  }
}
```
3. Abrir un navegador de internet y dirigirse a su repositorio en Github, en la sección *Settings*, buscar la opción *Secrets and Variables* y seleccionar la opción *Actions*. Dentro de esta hacer click en el botón *New Repository Secret*.
   ![image](https://github.com/UPT-FAING-EPIS/lab_cloud_01/assets/10199939/cb5a0c40-64ef-430a-b0bb-310a94d83364)

4. En el navegador, dentro de la ventana *New Secret*, colocar como nombre AZURE_USERNAME y como valor su cuenta de correo azure. Repetir el mismo proceso para crear otro secreto de nombre AZURE_USERPASS y como valor la contraseña de su cuenta de azure.

5. En el Visual Studio Code, crear la carpeta .github/workflows en la raiz del proyecto, seguidamente crear el archivo deploy.yml con el siguiente contenido
<details><summary>Click to expand: deploy.yml</summary>

```Yaml
name: Construcción Infrastructura en Azure

on:
  push:
    branches: [ "main" ]
    paths:
      - 'infra/**'
      - '.github/workflows/infra.yml'
  workflow_dispatch:

jobs:
  Deploy-Infra:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: login azure
        run: | 
          az login -u ${{ secrets.AWA_USERNAME }} -p ${{ secrets.AWA_PASSWORD }}

      - name: Setup tfsec
        run: |
            curl -L -o /tmp/tfsec_1.28.13_linux_amd64.tar.gz "https://github.com/aquasecurity/tfsec/releases/download/v1.28.13/tfsec_1.28.13_linux_amd64.tar.gz"
            tar -xzvf /tmp/tfsec_1.28.13_linux_amd64.tar.gz -C /tmp
            mv -v /tmp/tfsec /usr/local/bin/tfsec
            chmod +x /usr/local/bin/tfsec
      - name: tfsec
        run: |
          cd Infra
          /usr/local/bin/tfsec --format=markdown --out=tfsec.md .
          echo "## TFSec Output" >> $GITHUB_STEP_SUMMARY
          cat tfsec.md >> $GITHUB_STEP_SUMMARY
  
      - name: Setup Terraform
        uses: hashicorp/setup-terraform@v3
      - name: Terraform Init
        id: init
        run: cd Infra && terraform init 
    #   - name: Terraform Fmt
    #     id: fmt
    #     run: cd Infra && terraform fmt -check
      - name: Terraform Validate
        id: validate
        run: cd Infra && terraform validate -no-color
      - name: Terraform Plan
        run: cd Infra && terraform plan -no-color -out main.tfplan

      - name: Create String Output
        id: tf-plan-string
        run: |
            TERRAFORM_PLAN=$(cd Infra && terraform show -no-color main.tfplan)

            delimiter="$(openssl rand -hex 8)"
            echo "summary<<${delimiter}" >> $GITHUB_OUTPUT
            echo "## Terraform Plan Output" >> $GITHUB_OUTPUT
            echo "<details><summary>Click to expand</summary>" >> $GITHUB_OUTPUT
            echo "" >> $GITHUB_OUTPUT
            echo '```terraform' >> $GITHUB_OUTPUT
            echo "$TERRAFORM_PLAN" >> $GITHUB_OUTPUT
            echo '```' >> $GITHUB_OUTPUT
            echo "</details>" >> $GITHUB_OUTPUT
            echo "${delimiter}" >> $GITHUB_OUTPUT

      - name: Publish Terraform Plan to Task Summary
        env:
          SUMMARY: ${{ steps.tf-plan-string.outputs.summary }}
        run: |
          echo "$SUMMARY" >> $GITHUB_STEP_SUMMARY

      - name: Outputs
        id: vars
        run: |
            echo "terramaid_version=$(curl -s https://api.github.com/repos/RoseSecurity/Terramaid/releases/latest | grep tag_name | cut -d '"' -f 4)" >> $GITHUB_OUTPUT
            case "${{ runner.arch }}" in
            "X64" )
                echo "arch=x86_64" >> $GITHUB_OUTPUT
                ;;
            "ARM64" )
                echo "arch=arm64" >> $GITHUB_OUTPUT
                ;;
            esac

      - name: Setup Go
        uses: actions/setup-go@v5
        with:
          go-version: 'stable'

      - name: Setup Terramaid
        run: |
            curl -L -o /tmp/terramaid.tar.gz "https://github.com/RoseSecurity/Terramaid/releases/download/${{ steps.vars.outputs.terramaid_version }}/Terramaid_Linux_${{ steps.vars.outputs.arch }}.tar.gz"
            tar -xzvf /tmp/terramaid.tar.gz -C /tmp
            mv -v /tmp/Terramaid /usr/local/bin/terramaid
            chmod +x /usr/local/bin/terramaid

      - name: Terramaid
        id: terramaid
        run: |
            cd Infra
            /usr/local/bin/terramaid run

      - name: Publish graph in step comment
        run: |
            echo "## Terramaid Graph" >> $GITHUB_STEP_SUMMARY
            cat Infra/Terramaid.md >> $GITHUB_STEP_SUMMARY 

      - name: Setup Graphviz
        uses: ts-graphviz/setup-graphviz@v2        

      - name: Setup Inframap
        run: |
            curl -L -o /tmp/inframap.tar.gz "https://github.com/cycloidio/inframap/releases/download/v0.7.0/inframap-linux-amd64.tar.gz"
            tar -xzvf /tmp/inframap.tar.gz -C /tmp
            mv -v /tmp/inframap-linux-amd64 /usr/local/bin/inframap
            chmod +x /usr/local/bin/inframap
      - name: Inframap
        run: |
            cd Infra
            /usr/local/bin/inframap generate main.tf --raw | dot -Tsvg > inframap_azure.svg
      - name: Upload inframap
        id: inframap-upload-step
        uses: actions/upload-artifact@v4
        with:
          name: inframap_azure.svg
          path: Infra/inframap_azure.svg

      - name: Setup Infracost
        uses: infracost/actions/setup@v3
        with:
            api-key: ${{ secrets.INFRACOST_API_KEY }}
      - name: Infracost
        run: |
            cd Infra
            infracost breakdown --path . --format html --out-file infracost-report.html
            sed -i '19,137d' infracost-report.html
            sed -i 's/$0/$ 0/g' infracost-report.html

      - name: Convert HTML to Markdown
        id: html2markdown
        uses: rknj/html2markdown@v1.1.0
        with:
            html-file: "Infra/infracost-report.html"

      - name: Upload infracost report
        run: |
            echo "## Infracost Report" >> $GITHUB_STEP_SUMMARY
            echo "${{ steps.html2markdown.outputs.markdown-content }}" >> infracost.md
            cat infracost.md >> $GITHUB_STEP_SUMMARY

      - name: Terraform Apply
        run: |
            cd Infra
            terraform apply -auto-approve main.tfplan

```
</details>

6. En el Visual Studio Code, guardar los cambios y subir los cambios al repositorio. Revisar los logs de la ejeuciòn de automatizaciòn y anotar el numero de identificaciòn de Grupo de Recursos y Aplicación Web creados
```Bash
azurerm_linux_web_app.webapp: Creation complete after 53s [id=/subscriptions/1f57de72-50fd-4271-8ab9-3fc129f02bc0/resourceGroups/upt-arg-XXX/providers/Microsoft.Web/sites/upt-awa-XXX]
```

### CONSTRUCCION DE LA APLICACION

1. En el terminal, ubicarse en un ruta que no sea del sistema y ejecutar los siguientes comandos.
```Bash
dotnet new mvc -o src -n Shorten
cd src
dotnet tool install -g dotnet-aspnet-codegenerator --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.InMemory --version=8.0.0
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version=8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools --version=8.0.0
dotnet add package Microsoft.VisualStudio.Web.CodeGeneration.Design --version=8.0.0
dotnet add package Microsoft.AspNetCore.Components.QuickGrid --version=8.0.0
dotnet add package Microsoft.AspNetCore.Components.QuickGrid.EntityFrameworkAdapter --version=8.0.0
```
2. En el Visual Studio Code, en la carpeta src/Models, crear el archivo UrlMapping.cs con el siguiente contenido:
```CSharp
namespace Shorten.Models;
/// <summary>
/// Clase de dominio que representa una acortaciòn de url
/// </summary>
public class UrlMapping
{
    /// <summary>
    /// Identificador del mapeo de url
    /// </summary>
    /// <value>Entero</value>
    public int Id { get; set; }
    /// <summary>
    /// Valor original de la url
    /// </summary>
    /// <value>Cadena</value>
    public string OriginalUrl { get; set; } = string.Empty;
    /// <summary>
    /// Valor corto de la url
    /// </summary>
    /// <value>Cadena</value>
    public string ShortenedUrl { get; set; } = string.Empty;
}
```
  
3. En el Visual Studio Code, en la carpeta src/Models, crear el archivo ShortContext.cs con el siguiente contenido:
```CSharp
using Microsoft.EntityFrameworkCore;
namespace Shorten.Models;
/// <summary>
/// Clase de infraestructura que representa el contexto de la base de datos
/// </summary>
public class ShortContext : DbContext
{
    /// <summary>
    /// Constructor de la clase
    /// </summary>
    /// <param name="options">opciones de conexiòn de BD</param>
    public ShortContext(DbContextOptions<ShortContext> options) : base(options)
    {
    }
    /// <summary>
    /// Propiedad que representa la tabla de mapeo de urls
    /// </summary>
    /// <value>Conjunto de UrlMapping</value>
    public DbSet<UrlMapping> UrlMappings { get; set; }
}
```

4. En el Visual Studio Code, en la carpeta src, modificar el archivo Program.cs con el siguiente contenido al inicio:
```CSharp
using Shorten.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ShortContext>(options =>
    options.UseInMemoryDatabase("ShortDB"));
builder.Services.AddQuickGridEntityFrameworkAdapter();
var app = builder.Build();
...
```

5. En el terminal, ejecutar el siguiente comando para crear nu nuevo controlador y sus vistas asociadas.
```Powershell
dotnet aspnet-codegenerator Controller -name UrlMappingController -m UrlMapping -dc ShortContext -outDir Controllers -udl
```

6. En el Visual Studio Code, en la carpeta src, modificar el archivo _Layout.cshtml, Adicionando la siguiente opciòn dentro del navegador:
```CSharp
...
         <li class="nav-item">
             <a class="nav-link text-dark" asp-area="" asp-controller="UrlMapping" asp-action="Index">Shortener</a>
         </li>
...
```

### DESPLIEGUE DE LA APLICACION 

1. En el terminal, ejecutar el siguiente comando para obtener el perfil publico (Publish Profile) de la aplicación. Anotarlo porque se utilizara posteriormente.
```Powershell
az webapp deployment list-publishing-profiles --name upt-awa-XXX --resource-group upt-arg-XXX --xml
```
> Donde XXX; es el numero de identicación de la Aplicación Web creada en la primera sección

2. Abrir un navegador de internet y dirigirse a su repositorio en Github, en la sección *Settings*, buscar la opción *Secrets and Variables* y seleccionar la opción *Actions*. Dentro de esta hacer click en el botón *New Repository Secret*.
   ![image](https://github.com/UPT-FAING-EPIS/lab_cloud_01/assets/10199939/cb5a0c40-64ef-430a-b0bb-310a94d83364)

3. En el navegador, dentro de la ventana *New Secret*, colocar como nombre AZURE_WEBAPP_PUBLISH_PROFILE y como valor el obtenido en el paso 5.
   ![image](https://github.com/UPT-FAING-EPIS/lab_cloud_01/assets/10199939/c368f3fd-d9e4-4f21-bec4-727f267bcf74)
 
4. En el Visual Studio Code, dentro de la carpeta `.github/workflows`, crear el archivo ci-cd.yml con el siguiente contenido
```Yaml
name: Construcción y despliegue de una aplicación ASP.NET MVC a Azure

env:
  AZURE_WEBAPP_NAME: upt-awa-XXX          # Aqui va el nombre de su aplicación
  AZURE_WEBAPP_PACKAGE_PATH: '.'          # Es la ruta de destino
  DOTNET_VERSION: '8'                     # la versión de .NET

on:
  push:
    branches: [ "main" ]
  workflow_dispatch:
permissions:
  contents: read

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Configurando .NET Core
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      - uses: actions/setup-java@v4
        with:
          distribution: 'temurin'
          java-version: '17'
      - name: Restaurar los paquetes
        run: cd src && dotnet restore
      - name: Ejecutar pruebas
        run: cd src && dotnet test --collect:"XPlat Code Coverage"
      - name: ReportGenerator
        uses: danielpalme/ReportGenerator-GitHub-Action@5.3.7
        with:
          reports: ./src/*/*/*/coverage.cobertura.xml
          targetdir: coveragereport
          reporttypes: MarkdownSummary;MarkdownAssembliesSummary;MarkdownSummaryGithub
      - name: Upload coverage report artifact
        uses: actions/upload-artifact@v4
        with:
          name: CoverageReport 
          path: coveragereport 
      - name: Publish coverage in build summary # 
        run: cat coveragereport/SummaryGithub.md >> $GITHUB_STEP_SUMMARY 
        shell: bash
      - name: Instalar Scanner
        run: dotnet tool install -g dotnet-sonarscanner
      - name: Ejecutar escaneo
        run: |
          cd src
          dotnet-sonarscanner begin /k:"${{ env.SONAR_PROJECT }}" /o:"${{ env.SONAR_ORG }}" /d:sonar.login="${{ secrets.SONAR_TOKEN }}" /d:sonar.host.url="https://sonarcloud.io"
          dotnet build
          dotnet-sonarscanner end /d:sonar.login="${{ secrets.SONAR_TOKEN }}"
      - name: Publicar la aplicación de manera local
        run: cd src && dotnet publish -c Release -o ${{env.DOTNET_ROOT}}/publish
      - name: Subir el artefacto para el job de despliegue
        uses: actions/upload-artifact@v4
        with:
          name: .net-app
          path: ${{env.DOTNET_ROOT}}/publish

  deploy:
    permissions:
      contents: none
    runs-on: ubuntu-latest
    needs: build
    environment:
      name: 'Development'
      url: ${{ steps.deploy-to-webapp.outputs.webapp-url }}

    steps:
      - name: Descargar el artefacto desde el job de construccion
        uses: actions/download-artifact@v4
        with:
          name: .net-app
      - name: Desplegar a Azure Web App
        id: deploy-to-webapp
        uses: azure/webapps-deploy@v2
        with:
          app-name: ${{ env.AZURE_WEBAPP_NAME }}
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
          package: ${{ env.AZURE_WEBAPP_PACKAGE_PATH }}
          restart: true
```

5. En el Visual Studio Code o en el Terminal, confirmar los cambios con sistema de controlde versiones (git add ... git commit...) y luego subir esos cambios al repositorio remoto (git push ...).
   
6. En el Navegador de internet, dirigirse al repositorio de Github y revisar la seccion Actions, verificar que se esta ejecutando correctamente el Workflow
![image](https://github.com/UPT-FAING-EPIS/lab_cloud_01/assets/10199939/82e38848-2893-4bb0-913a-0d5b99e95ca5)

7. En el Navegador de internet, una vez finalizada la automatización, ingresar al sitio creado y navegar por el (https://upt-awa-XXX.azurewebsites.net).

8. En el Terminal, revisar las metricas de navegacion con el siguiente comando.
```Powershell
az monitor metrics list --resource "/subscriptions/XXXXXXXXXXXXXXX/resourceGroups/upt-arg-XXX/providers/Microsoft.Web/sites/upt-awa-XXXX" --metric "Requests" --start-time 2025-01-07T18:00:00Z --end-time 2025-01-07T23:00:00Z --output table
```
> Reemplazar los valores: 1. ID de suscripcion de Azure, 2. ID de creaciòn de infra y 3. El rango de fechas de uso de la aplicación.

7. En el Terminal, ejecutar el siguiente comando para obtener la plantilla de los recursos creados de azure en el grupo de recursos UPT.
```Powershell
az group export -n upt-arg-XXX > lab_01.json
```

8. En el Visual Studio Code, instalar la extensión *ARM Template Viewer*, abrir el archivo lab_01.json y hacer click en el icono de previsualizar ARM.
![image](https://github.com/UPT-FAING-EPIS/lab_cloud_01/assets/10199939/39ea1bdd-a0c7-482b-9417-5c7328e55198)


## ACTIVIDADES ENCARGADAS

1. Subir el diagrama al repositorio como lab_01.png y el reporte de metricas.
2. En el archivo main.tf, implementar el recurso azurerm_app_service_source_control, para el despliegue automatizado de la aplicación
3. Construir pruebas de interfaz para completar el 100% de cobertura de aplicación.
