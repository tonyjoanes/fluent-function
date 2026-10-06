# FluentFunctions infrastructure: a .NET isolated Function app on Flex Consumption, built from
# Azure Verified Modules. No connection strings or keys: the app reaches every dependency with a
# user-assigned managed identity, and local/shared-key auth is switched off wherever Azure allows.
#
# Keep in step with infra/bicep: see docs/iac-parity.md.

data "azurerm_client_config" "current" {}

data "azurerm_resource_group" "this" {
  name = var.resource_group_name
}

locals {
  location        = coalesce(var.location, data.azurerm_resource_group.this.location)
  resource_suffix = "${var.app_name}-${var.environment_name}"
  # Storage and Key Vault names are global and short; a hash of the resource group keeps them unique.
  unique_suffix             = substr(sha1(data.azurerm_resource_group.this.id), 0, 6)
  tags                      = merge(var.tags, { application = var.app_name, environment = var.environment_name, managed-by = "terraform" })
  deployment_container_name = "app-package"
  storage_account_name      = substr("st${var.app_name}${var.environment_name}${local.unique_suffix}", 0, 24)
  service_bus_name          = "sbns-${local.resource_suffix}-${local.unique_suffix}"

  roles = {
    storage_blob_data_owner        = "Storage Blob Data Owner"
    storage_queue_data_contributor = "Storage Queue Data Contributor"
    storage_table_data_contributor = "Storage Table Data Contributor"
    monitoring_metrics_publisher   = "Monitoring Metrics Publisher"
    key_vault_secrets_user         = "Key Vault Secrets User"
    service_bus_data_receiver      = "Azure Service Bus Data Receiver"
    service_bus_data_sender        = "Azure Service Bus Data Sender"
  }

  app_settings = merge(
    {
      # Identity-based host storage: no AzureWebJobsStorage connection string.
      AzureWebJobsStorage__accountName = module.storage.name
      AzureWebJobsStorage__credential  = "managedidentity"
      AzureWebJobsStorage__clientId    = module.identity.client_id
      # Used by the app's own Azure SDK clients and the OpenTelemetry exporter.
      AZURE_CLIENT_ID                           = module.identity.client_id
      APPLICATIONINSIGHTS_CONNECTION_STRING     = module.app_insights.connection_string
      APPLICATIONINSIGHTS_AUTHENTICATION_STRING = "ClientId=${module.identity.client_id};Authorization=AAD"
      KeyVault__Uri                             = module.key_vault.uri
    },
    //#if (UseServiceBus)
    {
      ServiceBusConnection__fullyQualifiedNamespace = "${local.service_bus_name}.servicebus.windows.net"
      ServiceBusConnection__credential              = "managedidentity"
      ServiceBusConnection__clientId                = module.identity.client_id
      ServiceBus__QueueName                         = "orders"
    },
    //#endif
    //#if (UseOrders)
    {
      Orders__MaxLines           = "50"
      Orders__MaxQuantityPerLine = "100"
      Orders__MaxAge             = "1.00:00:00"
    },
    //#endif
    //#if (UseTimer)
    {
      Cleanup__Schedule      = "0 0 2 * * *"
      Cleanup__RetentionDays = "30"
      Cleanup__BatchSize     = "500"
    },
    //#endif
  )
}

module "identity" {
  source  = "Azure/avm-res-managedidentity-userassignedidentity/azurerm"
  version = "0.5.3"

  name                = "id-${local.resource_suffix}"
  location            = local.location
  resource_group_name = var.resource_group_name
  tags                = local.tags
}

module "log_analytics" {
  source  = "Azure/avm-res-operationalinsights-workspace/azurerm"
  version = "0.5.1"

  name                                      = "log-${local.resource_suffix}"
  location                                  = local.location
  resource_group_name                       = var.resource_group_name
  log_analytics_workspace_retention_in_days = var.log_retention_days
  tags                                      = local.tags
}

module "app_insights" {
  source  = "Azure/avm-res-insights-component/azurerm"
  version = "0.4.0"

  name                          = "appi-${local.resource_suffix}"
  location                      = local.location
  resource_group_name           = var.resource_group_name
  workspace_id                  = module.log_analytics.resource_id
  local_authentication_disabled = true
  tags                          = local.tags

  role_assignments = {
    metrics_publisher = {
      role_definition_id_or_name = local.roles.monitoring_metrics_publisher
      principal_id               = module.identity.principal_id
      principal_type             = "ServicePrincipal"
    }
  }
}

module "storage" {
  source  = "Azure/avm-res-storage-storageaccount/azurerm"
  version = "0.10.0"

  name                            = local.storage_account_name
  location                        = local.location
  parent_id                       = data.azurerm_resource_group.this.id
  account_replication_type        = "LRS"
  shared_access_key_enabled       = false
  allow_nested_items_to_be_public = false
  public_network_access_enabled   = true
  min_tls_version                 = "TLS1_2"
  tags                            = local.tags

  network_rules = {
    default_action = "Allow"
    bypass         = ["AzureServices"]
  }

  containers = {
    app_package = {
      name = local.deployment_container_name
    }
  }

  role_assignments = {
    for key in ["storage_blob_data_owner", "storage_queue_data_contributor", "storage_table_data_contributor"] : key => {
      role_definition_id_or_name = local.roles[key]
      principal_id               = module.identity.principal_id
      principal_type             = "ServicePrincipal"
    }
  }
}

module "key_vault" {
  source  = "Azure/avm-res-keyvault-vault/azurerm"
  version = "0.11.0"

  name                          = substr("kv-${var.app_name}-${var.environment_name}-${local.unique_suffix}", 0, 24)
  location                      = local.location
  resource_group_name           = var.resource_group_name
  tenant_id                     = data.azurerm_client_config.current.tenant_id
  sku_name                      = "standard"
  purge_protection_enabled      = var.environment_name == "prod"
  soft_delete_retention_days    = 90
  public_network_access_enabled = true
  tags                          = local.tags

  network_acls = {
    bypass         = "AzureServices"
    default_action = "Allow"
  }

  role_assignments = {
    secrets_user = {
      role_definition_id_or_name = local.roles.key_vault_secrets_user
      principal_id               = module.identity.principal_id
      principal_type             = "ServicePrincipal"
    }
  }
}
//#if (UseServiceBus)

module "service_bus" {
  source  = "Azure/avm-res-servicebus-namespace/azurerm"
  version = "0.4.0"

  name                = local.service_bus_name
  location            = local.location
  resource_group_name = var.resource_group_name
  sku                 = "Standard"
  local_auth_enabled  = false
  tags                = local.tags

  queues = {
    orders = {
      max_delivery_count                   = 10
      lock_duration                        = "PT1M"
      dead_lettering_on_message_expiration = true
    }
  }

  role_assignments = {
    for key in ["service_bus_data_receiver", "service_bus_data_sender"] : key => {
      role_definition_id_or_name = local.roles[key]
      principal_id               = module.identity.principal_id
    }
  }
}
//#endif

module "plan" {
  source  = "Azure/avm-res-web-serverfarm/azurerm"
  version = "2.0.8"

  name                   = "asp-${local.resource_suffix}"
  location               = local.location
  parent_id              = data.azurerm_resource_group.this.id
  os_type                = "Linux"
  sku_name               = "FC1"
  zone_balancing_enabled = false
  worker_count           = 1
  tags                   = local.tags
}

module "function_app" {
  source  = "Azure/avm-res-web-site/azurerm"
  version = "0.23.0"

  name                     = "func-${local.resource_suffix}-${local.unique_suffix}"
  location                 = local.location
  parent_id                = data.azurerm_resource_group.this.id
  service_plan_resource_id = module.plan.resource_id
  kind                     = "functionapp"
  os_type                  = "Linux"
  https_only               = true
  tags                     = merge(local.tags, { azd-service-name = "functions" })

  managed_identities = {
    user_assigned_resource_ids = [module.identity.resource_id]
  }
  key_vault_reference_identity = module.identity.resource_id

  ftp_publish_basic_authentication_enabled = false
  scm_publish_basic_authentication_enabled = false

  # Flex Consumption
  function_app_uses_fc1             = true
  fc1_runtime_name                  = "dotnet-isolated"
  fc1_runtime_version               = "10.0"
  maximum_instance_count            = var.maximum_instance_count
  instance_memory_in_mb             = var.instance_memory_mb
  storage_container_type            = "blobContainer"
  storage_container_endpoint        = "https://${module.storage.name}.blob.core.windows.net/${local.deployment_container_name}"
  storage_authentication_type       = "UserAssignedIdentity"
  storage_user_assigned_identity_id = module.identity.resource_id

  site_config = {
    minimum_tls_version = "1.2"
    http2_enabled       = true
  }

  app_settings = local.app_settings
}
