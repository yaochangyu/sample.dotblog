# Tasks: Task Management Platform 集中管理平台

**Input**: Design documents from `/mnt/d/lab/sample.dotblog/AI.SDD/Lab.EventBus.SpecKit/specs/001-task-management-platform/`
**Prerequisites**: plan.md (✓), research.md (✓), data-model.md (✓), contracts/ (✓)

## Execution Flow (main)
```
1. Load plan.md from feature directory ✓
   → Extract: C# 11/.NET 8, ASP.NET Core, EF Core, MediatR, Clean Architecture
2. Load optional design documents ✓:
   → data-model.md: TaskEntity, TaskStatus, CallbackUrl, TaskData, repositories
   → contracts/openapi.yaml: 4 API endpoints (POST tasks, GET dequeue, POST execute, GET status)
   → research.md: HttpClient+Polly, In-Memory→Redis queue, TDD approach
3. Generate tasks by category ✓:
   → Setup: project structure, dependencies, EF Core migration
   → Tests: contract tests (4), integration tests (lifecycle scenarios)
   → Core: domain entities, application services, infrastructure
   → Integration: database, queue, HTTP clients, middleware
   → Polish: unit tests, performance validation, documentation
4. Apply task rules ✓:
   → Different projects/files = mark [P] for parallel
   → Same file = sequential (no [P])
   → Tests before implementation (TDD)
5. Number tasks sequentially (T001, T002...) ✓
6. Generate dependency graph ✓
7. Create parallel execution examples ✓
8. Validate task completeness ✓:
   → All contracts have tests? ✓ (4/4)
   → All entities have models? ✓ (TaskEntity + supporting types)
   → All endpoints implemented? ✓ (4/4)
9. Return: SUCCESS (tasks ready for execution) ✓
```

## Format: `[ID] [P?] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- Include exact file paths in descriptions

## Path Conventions
Based on plan.md Clean Architecture structure:
```
src/
├── Lab.TaskManagement.Domain/          # 領域層
├── Lab.TaskManagement.Application/     # 應用層
├── Lab.TaskManagement.Infrastructure/  # 基礎設施層
└── Lab.TaskManagement.WebApi/         # 展示層

tests/
├── Lab.TaskManagement.Domain.Tests/
├── Lab.TaskManagement.Application.Tests/
├── Lab.TaskManagement.Infrastructure.Tests/
├── Lab.TaskManagement.WebApi.Tests/
└── Lab.TaskManagement.Integration.Tests/
```

## Phase 3.1: Setup
- [ ] T001 Create Clean Architecture project structure with 4 layers in src/
- [ ] T002 Initialize .NET 8 solution with Lab.TaskManagement.* projects and NuGet dependencies
- [ ] T003 [P] Configure Entity Framework Core with SQL Server/PostgreSQL connection in Infrastructure layer
- [ ] T004 [P] Configure linting (dotnet format), StyleCop, and EditorConfig
- [ ] T005 Create database migration for TaskEntity schema in src/Lab.TaskManagement.Infrastructure/Migrations/

## Phase 3.2: Tests First (TDD) ⚠️ MUST COMPLETE BEFORE 3.3
**CRITICAL: These tests MUST be written and MUST FAIL before ANY implementation**

### Contract Tests
- [ ] T006 [P] Contract test POST /api/v1/tasks in tests/Lab.TaskManagement.WebApi.Tests/Controllers/TasksControllerContractTests.cs
- [ ] T007 [P] Contract test GET /api/v1/tasks/dequeue in tests/Lab.TaskManagement.WebApi.Tests/Controllers/DequeueContractTests.cs
- [ ] T008 [P] Contract test POST /api/v1/tasks/{id}/execute in tests/Lab.TaskManagement.WebApi.Tests/Controllers/ExecuteContractTests.cs
- [ ] T009 [P] Contract test GET /api/v1/tasks/{id} in tests/Lab.TaskManagement.WebApi.Tests/Controllers/StatusContractTests.cs

### Integration Tests
- [ ] T010 [P] Integration test complete task lifecycle (create→dequeue→execute→complete) in tests/Lab.TaskManagement.Integration.Tests/TaskLifecycleTests.cs
- [ ] T011 [P] Integration test callback execution with HttpClient in tests/Lab.TaskManagement.Integration.Tests/CallbackExecutionTests.cs
- [ ] T012 [P] Integration test task failure and retry mechanism in tests/Lab.TaskManagement.Integration.Tests/RetryMechanismTests.cs
- [ ] T013 [P] Integration test queue operations with concurrency in tests/Lab.TaskManagement.Integration.Tests/QueueConcurrencyTests.cs

## Phase 3.3: Core Implementation (ONLY after tests are failing)

### Domain Layer
- [ ] T014 [P] TaskStatus enum in src/Lab.TaskManagement.Domain/Enums/TaskStatus.cs
- [ ] T015 [P] CallbackUrl value object in src/Lab.TaskManagement.Domain/ValueObjects/CallbackUrl.cs
- [ ] T016 [P] TaskData value object in src/Lab.TaskManagement.Domain/ValueObjects/TaskData.cs
- [ ] T017 [P] BaseEntity abstract class in src/Lab.TaskManagement.Domain/Entities/BaseEntity.cs
- [ ] T018 TaskEntity with state transitions in src/Lab.TaskManagement.Domain/Entities/TaskEntity.cs
- [ ] T019 [P] Domain events (TaskCreated, TaskStatusChanged) in src/Lab.TaskManagement.Domain/Events/
- [ ] T020 [P] ITaskRepository interface in src/Lab.TaskManagement.Domain/Repositories/ITaskRepository.cs
- [ ] T021 [P] TaskDomainService in src/Lab.TaskManagement.Domain/Services/TaskDomainService.cs

### Application Layer
- [ ] T022 [P] CreateTaskCommand and Handler in src/Lab.TaskManagement.Application/Commands/CreateTask/
- [ ] T023 [P] DequeueTaskQuery and Handler in src/Lab.TaskManagement.Application/Queries/DequeueTask/
- [ ] T024 [P] ExecuteTaskCommand and Handler in src/Lab.TaskManagement.Application/Commands/ExecuteTask/
- [ ] T025 [P] GetTaskStatusQuery and Handler in src/Lab.TaskManagement.Application/Queries/GetTaskStatus/
- [ ] T026 [P] Task DTOs in src/Lab.TaskManagement.Application/DTOs/
- [ ] T027 [P] ICallbackService interface in src/Lab.TaskManagement.Application/Services/ICallbackService.cs

### Infrastructure Layer
- [ ] T028 [P] TaskRepository implementation in src/Lab.TaskManagement.Infrastructure/Persistence/TaskRepository.cs
- [ ] T029 [P] TaskEntityConfiguration for EF Core in src/Lab.TaskManagement.Infrastructure/Persistence/Configurations/TaskEntityConfiguration.cs
- [ ] T030 [P] TaskManagementDbContext in src/Lab.TaskManagement.Infrastructure/Persistence/TaskManagementDbContext.cs
- [ ] T031 [P] In-Memory TaskQueueService in src/Lab.TaskManagement.Infrastructure/Queue/InMemoryTaskQueueService.cs
- [ ] T032 [P] CallbackService with HttpClient+Polly in src/Lab.TaskManagement.Infrastructure/Http/CallbackService.cs
- [ ] T033 [P] DependencyInjection registration in src/Lab.TaskManagement.Infrastructure/DependencyInjection.cs

### WebApi Layer
- [ ] T034 TasksController POST /api/v1/tasks endpoint
- [ ] T035 TasksController GET /api/v1/tasks/dequeue endpoint
- [ ] T036 TasksController POST /api/v1/tasks/{id}/execute endpoint
- [ ] T037 TasksController GET /api/v1/tasks/{id} endpoint
- [ ] T038 [P] Global exception handling middleware in src/Lab.TaskManagement.WebApi/Middleware/ExceptionMiddleware.cs
- [ ] T039 [P] Structured logging configuration in src/Lab.TaskManagement.WebApi/Program.cs

## Phase 3.4: Integration
- [ ] T040 Connect TaskRepository to EF Core DbContext with dependency injection
- [ ] T041 Configure MediatR pipeline with validation and logging behaviors
- [ ] T042 [P] Health checks for database and queue in src/Lab.TaskManagement.WebApi/HealthChecks/
- [ ] T043 [P] OpenAPI/Swagger configuration matching contracts/openapi.yaml
- [ ] T044 [P] CORS and security headers configuration

## Phase 3.5: Polish
- [ ] T045 [P] Unit tests for TaskEntity state transitions in tests/Lab.TaskManagement.Domain.Tests/Entities/TaskEntityTests.cs
- [ ] T046 [P] Unit tests for CallbackUrl validation in tests/Lab.TaskManagement.Domain.Tests/ValueObjects/CallbackUrlTests.cs
- [ ] T047 [P] Unit tests for command handlers in tests/Lab.TaskManagement.Application.Tests/Commands/
- [ ] T048 [P] Unit tests for query handlers in tests/Lab.TaskManagement.Application.Tests/Queries/
- [ ] T049 Performance tests validating 1000+ req/s target using NBomber
- [ ] T050 [P] Update API documentation to match implemented endpoints
- [ ] T051 Execute quickstart.md validation scenarios (workflow test script)
- [ ] T052 [P] Code cleanup and remove duplication following DRY principles

## Dependencies

### Critical Path
1. **Setup** (T001-T005) must complete first
2. **Tests** (T006-T013) must complete and FAIL before implementation starts
3. **Domain Foundation** (T014-T017) → **Domain Logic** (T018-T021)
4. **Application Layer** (T022-T027) depends on Domain completion
5. **Infrastructure** (T028-T033) can run parallel to Application
6. **WebApi** (T034-T039) depends on Application + Infrastructure
7. **Integration** (T040-T044) requires all layers
8. **Polish** (T045-T052) requires working implementation

### Blocking Dependencies
- T018 blocks T019, T020, T021 (BaseEntity required)
- T020 blocks T028 (interface before implementation)
- T022-T025 block T034-T037 (handlers before controllers)
- T030 blocks T040 (DbContext before integration)
- T032 blocks T041 (services before pipeline configuration)

### Non-Blocking (Parallel Opportunities)
- Domain entities (T014-T017) can all run parallel
- Application commands/queries (T022-T025) can run parallel after domain
- Infrastructure services (T028, T031, T032) can run parallel
- Unit test files (T045-T048) can all run parallel
- Documentation tasks (T050, T051) can run parallel

## Parallel Execution Examples

### Phase 3.2 - All Contract Tests Together
```bash
# Launch T006-T009 simultaneously (different test files):
Task: "Contract test POST /api/v1/tasks in tests/Lab.TaskManagement.WebApi.Tests/Controllers/TasksControllerContractTests.cs"
Task: "Contract test GET /api/v1/tasks/dequeue in tests/Lab.TaskManagement.WebApi.Tests/Controllers/DequeueContractTests.cs"
Task: "Contract test POST /api/v1/tasks/{id}/execute in tests/Lab.TaskManagement.WebApi.Tests/Controllers/ExecuteContractTests.cs"
Task: "Contract test GET /api/v1/tasks/{id} in tests/Lab.TaskManagement.WebApi.Tests/Controllers/StatusContractTests.cs"

# Launch T010-T013 simultaneously (different integration test files):
Task: "Integration test complete task lifecycle in tests/Lab.TaskManagement.Integration.Tests/TaskLifecycleTests.cs"
Task: "Integration test callback execution in tests/Lab.TaskManagement.Integration.Tests/CallbackExecutionTests.cs"
Task: "Integration test retry mechanism in tests/Lab.TaskManagement.Integration.Tests/RetryMechanismTests.cs"
Task: "Integration test queue concurrency in tests/Lab.TaskManagement.Integration.Tests/QueueConcurrencyTests.cs"
```

### Phase 3.3 - Domain Foundation Parallel
```bash
# Launch T014-T017 simultaneously (different files in Domain layer):
Task: "TaskStatus enum in src/Lab.TaskManagement.Domain/Enums/TaskStatus.cs"
Task: "CallbackUrl value object in src/Lab.TaskManagement.Domain/ValueObjects/CallbackUrl.cs"
Task: "TaskData value object in src/Lab.TaskManagement.Domain/ValueObjects/TaskData.cs"
Task: "BaseEntity abstract class in src/Lab.TaskManagement.Domain/Entities/BaseEntity.cs"
```

### Phase 3.5 - Unit Tests Parallel
```bash
# Launch T045-T048 simultaneously (different test projects):
Task: "Unit tests for TaskEntity in tests/Lab.TaskManagement.Domain.Tests/Entities/TaskEntityTests.cs"
Task: "Unit tests for CallbackUrl in tests/Lab.TaskManagement.Domain.Tests/ValueObjects/CallbackUrlTests.cs"
Task: "Unit tests for command handlers in tests/Lab.TaskManagement.Application.Tests/Commands/"
Task: "Unit tests for query handlers in tests/Lab.TaskManagement.Application.Tests/Queries/"
```

## Validation Checklist
*GATE: Verified for task completeness*

- [x] All contracts have corresponding tests (T006-T009: 4/4 endpoints)
- [x] All entities have model tasks (TaskEntity + supporting value objects)
- [x] All tests come before implementation (Phase 3.2 before 3.3)
- [x] Parallel tasks truly independent (verified file paths)
- [x] Each task specifies exact file path
- [x] No task modifies same file as another [P] task
- [x] TDD approach enforced (failing tests required before implementation)
- [x] Clean Architecture layers properly separated
- [x] Performance requirements addressed (T049: 1000+ req/s validation)

## Notes
- [P] tasks = different files/projects, no dependencies - can run in parallel
- Verify tests fail before implementing (TDD critical)
- Commit after each task completion
- Follow Clean Architecture dependency rules (inner layers don't depend on outer)
- Use MediatR for application layer command/query handling
- Entity Framework Core for data persistence
- HttpClient + Polly for resilient callback execution
- Maintain task state machine integrity in domain layer