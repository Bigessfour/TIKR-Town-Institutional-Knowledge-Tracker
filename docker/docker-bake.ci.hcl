# CI-only Bake file. Context is the repo root.
# docker-compose.yml uses context ".." relative to docker/, which Bake
# resolves from the working directory and would read the parent folder.

target "tikr-api" {
  context    = "."
  dockerfile = "docker/Dockerfile.api"
  tags       = ["tikr-ci-api:local"]
  cache-from = ["type=gha,scope=tikr-api"]
  cache-to   = ["type=gha,scope=tikr-api,mode=max"]
}

target "tikr-web" {
  context    = "."
  dockerfile = "docker/Dockerfile.web"
  tags       = ["tikr-ci-web:local"]
  cache-from = ["type=gha,scope=tikr-web"]
  cache-to   = ["type=gha,scope=tikr-web,mode=max"]
}

group "smoke" {
  targets = ["tikr-api", "tikr-web"]
}
