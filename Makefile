.PHONY: help build run test watch clean reset-db migrate new-migration restore lint

# Default target - show help
help:
	@echo "TodoApp Development Commands"
	@echo ""
	@echo "Usage: make [target]"
	@echo ""
	@echo "Targets:"
	@echo "  build          Build the solution"
	@echo "  run            Run the web app (http://localhost:5000)"
	@echo "  test           Run all unit tests"
	@echo "  test-verbose   Run tests with detailed output"
	@echo "  watch          Run with hot reload (auto-restart on changes)"
	@echo "  restore        Restore NuGet packages"
	@echo "  clean          Clean build artifacts"
	@echo ""
	@echo "Database:"
	@echo "  migrate        Apply pending migrations"
	@echo "  new-migration  Create a new migration (usage: make new-migration NAME=MigrationName)"
	@echo "  reset-db       Delete database and re-apply migrations"
	@echo "  db-status      Show migration status"
	@echo ""
	@echo "Other:"
	@echo "  lint           Check for build warnings"
	@echo "  publish        Build for production"

# Build
build:
	dotnet build

# Run the app
run:
	cd TodoApp.Web && dotnet run --urls "http://localhost:5000"

# Run with hot reload
watch:
	cd TodoApp.Web && dotnet watch run --urls "http://localhost:5000"

# Run tests
test:
	dotnet test

# Run tests with verbose output
test-verbose:
	dotnet test --verbosity normal

# Restore packages
restore:
	dotnet restore

# Clean build artifacts
clean:
	dotnet clean
	rm -rf TodoApp.Web/bin TodoApp.Web/obj
	rm -rf TodoApp.Web.Tests/bin TodoApp.Web.Tests/obj

# Apply migrations
migrate:
	cd TodoApp.Web && dotnet ef database update

# Create new migration (usage: make new-migration NAME=AddNewFeature)
new-migration:
ifndef NAME
	$(error NAME is required. Usage: make new-migration NAME=MigrationName)
endif
	cd TodoApp.Web && dotnet ef migrations add $(NAME)

# Show migration status
db-status:
	cd TodoApp.Web && dotnet ef migrations list

# Reset database (delete and recreate)
reset-db:
	@echo "Deleting database..."
	rm -f TodoApp.Web/TodoApp.db TodoApp.Web/TodoApp.db-shm TodoApp.Web/TodoApp.db-wal
	@echo "Applying migrations..."
	cd TodoApp.Web && dotnet ef database update
	@echo "Database reset complete."

# Check for warnings
lint:
	dotnet build --warnaserrors

# Build for production
publish:
	dotnet publish -c Release -o ./publish
