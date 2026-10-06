output "function_app_name" {
  value = module.function_app.name
}

output "identity_client_id" {
  value = module.identity.client_id
}

output "key_vault_uri" {
  value = module.key_vault.uri
}
