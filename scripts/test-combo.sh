#!/usr/bin/env bash
# Generates one template combination into <dir> and checks it. Called by test-template.sh.
# Usage: scripts/test-combo.sh <dir> [dotnet new options...]
set -euo pipefail

dir="$1"
shift

need() {
  if command -v "$1" > /dev/null; then return 0; fi
  if [ "${CI:-}" = "true" ]; then echo "::error::$1 is required in CI"; exit 1; fi
  echo "  (skipping: $1 not installed)"
  return 1
}

bicep_cli() {
  if command -v bicep > /dev/null; then bicep "$@"; else az bicep "$@"; fi
}

dotnet new fluentfn --name Contoso.Orders --output "$dir" "$@"
cd "$dir"

# No template tokens may survive generation.
if grep -rnE '#if \(|//#if|fnappname|FluentFunctions' --exclude-dir=bin --exclude-dir=obj . ; then
  echo "Template tokens left in generated output"
  exit 1
fi

dotnet build --configuration Release
dotnet test --configuration Release --no-build

if [ -d infra/bicep ] && { command -v bicep > /dev/null || command -v az > /dev/null || need bicep; }; then
  bicep_cli lint infra/bicep/main.bicep
  for p in infra/bicep/*.bicepparam; do bicep_cli build-params "$p" --stdout > /dev/null; done
fi

if [ -d infra/terraform ] && need terraform; then
  terraform -chdir=infra/terraform fmt -check -diff
  terraform -chdir=infra/terraform init -backend=false -input=false > /dev/null
  terraform -chdir=infra/terraform validate
fi

if [ -d pipelines ]; then
  python3 -c 'import sys, yaml; [yaml.safe_load(open(f)) for f in sys.argv[1:]]' $(find pipelines -name '*.yml')
fi
