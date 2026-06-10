#!/usr/bin/env bash
# Seed CRM : produits suivis "pour de vrai" (à lancer contre une instance Outlet.Crm.Web).
# Idempotent côté trackers : re-tracker un package déjà suivi renvoie une erreur métier inoffensive.
set -euo pipefail
API="${CRM_API:-http://localhost:5000}"

create_product() { # name description -> id (réutilise le produit existant du même nom)
  local name="$1" desc="$2"
  local id
  id=$(curl -sf "$API/api/products/" | python3 -c "
import sys, json
for p in json.load(sys.stdin):
    if p['name'] == '$name':
        print(p['id']); break")
  if [ -z "$id" ]; then
    id=$(curl -sf -X POST "$API/api/products" -H 'Content-Type: application/json' \
      -d "{\"name\":\"$name\",\"description\":\"$desc\"}" | python3 -c 'import sys,json;print(json.load(sys.stdin)["id"])')
  fi
  echo "$id"
}

CLI=$(create_product "Outlet CLI" "CLI .NET open source")
curl -s -X POST "$API/api/products/$CLI/packages" -H 'Content-Type: application/json' \
  -d '{"registry":"Npm","packageId":"outlet-cli"}' -o /dev/null
curl -s -X POST "$API/api/products/$CLI/repositories" -H 'Content-Type: application/json' \
  -d '{"repository":"Leroy-Florian/Outlet-CLI"}' -o /dev/null
echo "Outlet CLI  ($CLI) : npm outlet-cli + github Leroy-Florian/Outlet-CLI"

PDF=$(create_product "FluentPdf" "Génération PDF fluide pour .NET")
curl -s -X POST "$API/api/products/$PDF/packages" -H 'Content-Type: application/json' \
  -d '{"registry":"NuGet","packageId":"FluentPdf"}' -o /dev/null
echo "FluentPdf   ($PDF) : nuget FluentPdf"
