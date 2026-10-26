#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/../.." && pwd)"
compose_file="$script_dir/docker-compose.gitlab.yml"
compose_project="vcsmanager-gitlab-integration"
port="${VCS_GITLAB_TEST_PORT:-8929}"
url="${VCS_GITLAB_TEST_URL:-}"
token="${VCS_GITLAB_TEST_TOKEN:-}"
namespace="${VCS_GITLAB_TEST_NAMESPACE:-vcsmanager-integration}"
started=0

cleanup() {
    if [[ "$started" -eq 1 ]]; then
        echo "Stopping the disposable GitLab instance..."
        docker compose -p "$compose_project" -f "$compose_file" down -v --remove-orphans \
            >/dev/null 2>&1 || true
    fi
}

trap cleanup EXIT

if [[ -n "$url" && -z "$token" ]]; then
    echo "SKIP: VCS_GITLAB_TEST_URL is set but VCS_GITLAB_TEST_TOKEN is missing."
    exit 0
fi

if [[ -z "$token" ]]; then
    if ! command -v docker >/dev/null 2>&1; then
        echo "SKIP: Docker is not installed and VCS_GITLAB_TEST_TOKEN is not set."
        exit 0
    fi

    if ! docker info >/dev/null 2>&1; then
        echo "SKIP: Docker is not running and VCS_GITLAB_TEST_TOKEN is not set."
        exit 0
    fi

    echo "Starting a disposable GitLab CE instance..."
    VCS_GITLAB_TEST_PORT="$port" \
        docker compose -p "$compose_project" -f "$compose_file" up -d --remove-orphans
    started=1

    echo "Waiting for GitLab to become ready (this can take several minutes)..."
    ready=0
    for _ in $(seq 1 240); do
        if curl -fsS "http://localhost:$port/-/readiness?all=1" >/dev/null 2>&1; then
            ready=1
            break
        fi
        sleep 5
    done

    if [[ "$ready" -ne 1 ]]; then
        echo "ERROR: GitLab did not become ready at http://localhost:$port." >&2
        exit 1
    fi

    token="glpat-$(openssl rand -hex 20)"
    echo "Provisioning the integration group and access token..."

    provisioned="$(
        docker compose -p "$compose_project" -f "$compose_file" exec -T \
            -e VCS_IT_TOKEN="$token" \
            -e VCS_IT_NAMESPACE="$namespace" \
            gitlab gitlab-rails runner '
user = User.find_by_username("root") || raise("root user not found")
organization = if Organizations::Organization.respond_to?(:default_organization)
  Organizations::Organization.default_organization
else
  Organizations::Organization.first
end
group = Group.find_by_full_path(ENV.fetch("VCS_IT_NAMESPACE"))
unless group
  group = Group.new(
    name: "VcsManager Integration",
    path: ENV.fetch("VCS_IT_NAMESPACE"),
    visibility_level: 0,
    organization: organization
  )
  group.save!
  group.add_owner(user)
end
user.personal_access_tokens.where(name: "vcsmanager-integration").destroy_all
token = user.personal_access_tokens.create!(
  name: "vcsmanager-integration",
  scopes: ["api"],
  expires_at: 1.day.from_now
)
token.set_token(ENV.fetch("VCS_IT_TOKEN"))
token.save!
puts token.token
'
    )"

    token="$(printf '%s\n' "$provisioned" | tail -n 1 | tr -d '\r')"
    if [[ -z "$token" ]]; then
        echo "ERROR: GitLab did not return an access token." >&2
        exit 1
    fi

    url="http://localhost:$port"
fi

echo "Running the GitLab integration tests."
VCS_GITLAB_TEST_URL="$url" \
VCS_GITLAB_TEST_TOKEN="$token" \
VCS_GITLAB_TEST_NAMESPACE="$namespace" \
    dotnet run --project "$repo_root/tests/VcsManager.IntegrationTests" -- --gitlab
