#!/usr/bin/env bash
#
# Outlet platform private registry — build & publish.
#
# Subcommands:
#   build              Validate every <item>.registry.json (files must exist on disk —
#                      "the manifest never lies"), then generate dist/registry/:
#                        - dist/registry/registry.json   the aggregate index
#                        - dist/registry/<item>/<file>   each shipped source file
#                      This layout is exactly what the CLI's HttpRegistrySource serves,
#                      so a committed dist/ doubles as the git-raw BOOTSTRAP source.
#
#   publish            Run build, then push every item to the Outlet Cloud ingestion API
#                      (the DISTRIBUTION plane). Contracts are published before adapters
#                      so dependencies always resolve.
#                        login -> resolve org by slug -> POST .../registry/items
#
# Publish environment (required unless --dry-run):
#   OUTLET_CLOUD_URL        e.g. https://cloud.outlet.dev   (no trailing slash needed)
#   OUTLET_CLOUD_EMAIL      service account (Owner/Admin of the org, Pro plan)
#   OUTLET_CLOUD_PASSWORD   service account password
#   OUTLET_ORG_SLUG         target organization slug, e.g. "acme"
#
# Flags:
#   --dry-run          For publish: print what would be POSTed (item + file count), no network.
#
# Exit codes: 0 ok, 1 usage/validation error, 2 publish/HTTP error.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
REGISTRY_ROOT="${REGISTRY_ROOT:-$ROOT/registry}"
DIST_DIR="${DIST_DIR:-$ROOT/dist/registry}"

die() { echo "error: $*" >&2; exit "${2:-1}"; }
log() { echo ">> $*" >&2; }

# --- discovery -------------------------------------------------------------

# Emits manifest paths, contracts first then adapters, each group name-sorted, so
# registryDependencies (adapter -> contract) always resolve at the receiving end.
ordered_manifests() {
  local manifests
  mapfile -t manifests < <(find "$REGISTRY_ROOT" -name '*.registry.json' | sort)
  [ "${#manifests[@]}" -gt 0 ] || die "no *.registry.json found under '$REGISTRY_ROOT'"
  local m
  for m in "${manifests[@]}"; do [ "$(jq -r .type "$m")" = "outlet:contract" ] && echo "$m"; done
  for m in "${manifests[@]}"; do [ "$(jq -r .type "$m")" = "outlet:adapter"  ] && echo "$m"; done
}

# --- build -----------------------------------------------------------------

build() {
  log "building registry from '$REGISTRY_ROOT'"
  rm -rf "$DIST_DIR"
  mkdir -p "$DIST_DIR"

  local index_tmp seen=() m name dir p
  index_tmp="$(mktemp -d)"

  while IFS= read -r m; do
    jq empty "$m" 2>/dev/null || die "$m: not valid JSON"
    name="$(jq -r '.name // empty' "$m")"
    [ -n "$name" ] || die "$m: missing 'name'"

    # unique names — a duplicate would silently overwrite a sibling
    local s; for s in "${seen[@]:-}"; do [ "$s" = "$name" ] && die "duplicate item name '$name'"; done
    seen+=("$name")

    dir="$(dirname "$m")"
    while IFS= read -r p; do
      [ -n "$p" ] || die "$name: a file entry has an empty 'path'"
      [ -f "$dir/$p" ] || die "$name declares file '$p' missing on disk"
      mkdir -p "$DIST_DIR/$name/$(dirname "$p")"
      cp "$dir/$p" "$DIST_DIR/$name/$p"
    done < <(jq -r '.files[].path' "$m")

    # index entry: drop the editor-only $schema, add the computed isContract flag
    jq '. + {isContract: (.type == "outlet:contract")} | del(.["$schema"])' "$m" \
      > "$index_tmp/$name.json"
    log "  + $name ($(jq '.files | length' "$m") file(s))"
  done < <(ordered_manifests)

  jq -s '{items: .}' "$index_tmp"/*.json > "$DIST_DIR/registry.json"
  rm -rf "$index_tmp"
  log "wrote $DIST_DIR/registry.json"
}

# --- publish ---------------------------------------------------------------

# Builds the {name, manifest, files:[{path,content}]} body the cloud API expects.
item_body() {
  local m="$1" dir name files_json="[]" p content
  dir="$(dirname "$m")"; name="$(jq -r .name "$m")"
  while IFS= read -r p; do
    content="$(jq -Rs '.' < "$dir/$p")"   # whole file -> JSON string
    files_json="$(jq -c --arg path "$p" --argjson content "$content" \
      '. + [{path: $path, content: $content}]' <<<"$files_json")"
  done < <(jq -r '.files[].path' "$m")
  jq -nc --arg name "$name" \
        --argjson manifest "$(jq -c 'del(.["$schema"])' "$m")" \
        --argjson files "$files_json" \
        '{name: $name, manifest: $manifest, files: $files}'
}

publish() {
  local dry_run="${1:-}"
  build

  if [ "$dry_run" = "--dry-run" ]; then
    local m
    while IFS= read -r m; do
      log "DRY-RUN would publish '$(jq -r .name "$m")' ($(jq '.files|length' "$m") file(s)) [$(jq -r .type "$m")]"
    done < <(ordered_manifests)
    log "dry-run complete — no network calls made"
    return 0
  fi

  : "${OUTLET_CLOUD_URL:?set OUTLET_CLOUD_URL}"
  : "${OUTLET_CLOUD_EMAIL:?set OUTLET_CLOUD_EMAIL}"
  : "${OUTLET_CLOUD_PASSWORD:?set OUTLET_CLOUD_PASSWORD}"
  : "${OUTLET_ORG_SLUG:?set OUTLET_ORG_SLUG}"
  local base="${OUTLET_CLOUD_URL%/}"
  local jar; jar="$(mktemp)"
  trap 'rm -f "$jar"' RETURN

  log "logging in to $base as $OUTLET_CLOUD_EMAIL"
  curl -fsS -c "$jar" -X POST "$base/auth/login" -H 'content-type: application/json' \
    -d "$(jq -nc --arg e "$OUTLET_CLOUD_EMAIL" --arg p "$OUTLET_CLOUD_PASSWORD" '{email:$e, password:$p}')" \
    >/dev/null || die "login failed" 2

  log "resolving organization '$OUTLET_ORG_SLUG'"
  local org_id
  org_id="$(curl -fsS -b "$jar" "$base/organizations/" \
    | jq -r --arg s "$OUTLET_ORG_SLUG" '.[] | select(.slug == $s) | .organizationId')" \
    || die "could not list organizations" 2
  [ -n "$org_id" ] || die "you are not a member of org '$OUTLET_ORG_SLUG' (or it does not exist)" 2

  local m name
  while IFS= read -r m; do
    name="$(jq -r .name "$m")"
    log "publishing '$name' -> org $org_id"
    item_body "$m" \
      | curl -fsS -b "$jar" -X POST "$base/organizations/$org_id/registry/items" \
          -H 'content-type: application/json' --data-binary @- >/dev/null \
      || die "publishing '$name' failed" 2
  done < <(ordered_manifests)

  log "published $(ordered_manifests | wc -l | tr -d ' ') item(s) to '$OUTLET_ORG_SLUG'"
}

# --- entrypoint ------------------------------------------------------------

case "${1:-}" in
  build)   build ;;
  publish) publish "${2:-}" ;;
  *) echo "usage: $0 {build | publish [--dry-run]}" >&2; exit 1 ;;
esac
