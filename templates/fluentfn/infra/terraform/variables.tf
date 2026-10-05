variable "resource_group_name" {
  type        = string
  description = "Existing resource group to deploy into."
}

variable "app_name" {
  type        = string
  default     = "fnappname"
  description = "Short application name used in resource names. Lowercase letters and digits."

  validation {
    condition     = can(regex("^[a-z0-9]{2,12}$", var.app_name))
    error_message = "app_name must be 2-12 lowercase letters or digits."
  }
}

variable "environment_name" {
  type        = string
  description = "Deployment environment."

  validation {
    condition     = contains(["dev", "test", "prod"], var.environment_name)
    error_message = "environment_name must be dev, test or prod."
  }
}

variable "location" {
  type        = string
  default     = null
  description = "Azure region for all resources. Defaults to the resource group's region."
}

variable "maximum_instance_count" {
  type        = number
  default     = 100
  description = "Maximum instances Flex Consumption may scale out to."
}

variable "instance_memory_mb" {
  type        = number
  default     = 2048
  description = "Memory per instance in MB."

  validation {
    condition     = contains([512, 2048, 4096], var.instance_memory_mb)
    error_message = "instance_memory_mb must be 512, 2048 or 4096."
  }
}

variable "log_retention_days" {
  type        = number
  default     = 30
  description = "Log Analytics retention in days."
}

variable "tags" {
  type        = map(string)
  default     = {}
  description = "Tags applied to every resource."
}
