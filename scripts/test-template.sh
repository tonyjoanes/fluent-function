#!/usr/bin/env bash
# Generates the template with every supported option combination, then builds, tests and
# validates the infrastructure of each one. This is what keeps the template trustworthy:
# run it locally before a PR, and CI runs it on every push.
#
# Requires: .NET 10 SDK. Optional: bicep (or az), terraform. Missing IaC tools are reported
# and skipped locally, but CI=true makes them mandatory.
#
# Usage: scripts/test-template.sh [filter]   e.g. scripts/test-template.sh terraform
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
out="${OUT_DIR:-$root/artifacts/template-tests}"
filter="${1:-}"

# name | dotnet new options
combos=(
  "http-bicep|--trigger http --iac bicep"
  "servicebus-bicep|--trigger servicebus --iac bicep"
  "timer-bicep|--trigger timer --iac bicep"
  "all-bicep|--trigger http --trigger servicebus --trigger timer --iac bicep"
  "http-terraform|--trigger http --iac terraform"
  "servicebus-terraform|--trigger servicebus --iac terraform"
  "timer-terraform|--trigger timer --iac terraform"
  "all-terraform|--trigger http --trigger servicebus --trigger timer --iac terraform"
  "all-noiac|--trigger http --trigger servicebus --trigger timer --iac none"
  "http-bare|--trigger http --iac none --pipeline none"
)

echo "Installing template from $root/templates/fluentfn"
dotnet new uninstall "$root/templates/fluentfn" > /dev/null 2>&1 || true
dotnet new install "$root/templates/fluentfn" > /dev/null

rm -rf "$out"
mkdir -p "$out"
failed=()

for combo in "${combos[@]}"; do
  name="${combo%%|*}"
  opts="${combo#*|}"
  [[ -n "$filter" && "$name" != *"$filter"* ]] && continue

  echo "::group::$name ($opts)"
  dir="$out/$name"
  # shellcheck disable=SC2086
  if ! "$root/scripts/test-combo.sh" "$dir" $opts; then
    failed+=("$name")
  fi
  echo "::endgroup::"
done

dotnet new uninstall "$root/templates/fluentfn" > /dev/null 2>&1 || true

if [ ${#failed[@]} -gt 0 ]; then
  echo "Failed combinations: ${failed[*]}"
  exit 1
fi
echo "All combinations passed."
