.PHONY: validate test lint build release seed serve build-web publish

TF_DIR      := infra
FRONTEND_DIR := web

validate: lint build test ## CI entry point — lint, build, test, and validate Terraform
	terraform -chdir=$(TF_DIR) init -input=false
	terraform -chdir=$(TF_DIR) validate

test: ## Run tests (none defined yet)
	@echo "No tests defined yet."

lint: ## Check Terraform formatting
	terraform fmt -check -recursive

build: build-web ## Build all artifacts (backend Lambda + frontend)
	dotnet publish src/Backend.csproj -c Release --no-self-contained -o infra/lambda

release: ## Release artifacts (none defined yet)
	@echo "No release steps defined yet."

seed: ## Seed local DynamoDB with sample data (requires DYNAMODB_ENDPOINT)
	bash scripts/seed-local.sh

serve: ## Run DynamoDB, the API, and the frontend locally
	bash scripts/serve-local.sh

build-web: ## Build the Vue frontend for production
	npm --prefix $(FRONTEND_DIR) install
	npm --prefix $(FRONTEND_DIR) run build

publish: build-web ## Publish frontend to S3 and invalidate CloudFront (requires FRONTEND_BUCKET and CF_DISTRIBUTION_ID)
	aws s3 sync $(FRONTEND_DIR)/dist/ s3://$(FRONTEND_BUCKET)/ --delete
	aws cloudfront create-invalidation --distribution-id $(CF_DISTRIBUTION_ID) --paths "/*"
