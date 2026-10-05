terraform {
  required_version = ">= 1.11, < 2.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.81"
    }
    azapi = {
      source  = "Azure/azapi"
      version = "~> 2.12"
    }
  }

  # State lives in an Azure Storage account the pipeline configures with -backend-config,
  # authenticating with Entra ID (use_azuread_auth) rather than an access key.
  backend "azurerm" {
    use_azuread_auth = true
  }
}

provider "azurerm" {
  features {}
  storage_use_azuread = true
}

provider "azapi" {}
