TAG ?= 2025

deploy:
    docker buildx build --platform linux/amd64,linux/arm64 -t tuso/paris-api:${TAG} --push .

.PHONY: deploy