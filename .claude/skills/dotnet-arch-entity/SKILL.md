---
name: dotnet-arch-entity
description: Guides the implementation of a new entity following the Clean Architecture pattern of this project. Covers all layers from DTO to Database, including EF Core Code First, AutoMapper profiles, Repository pattern, Domain services, and DI registration. Use when creating or modifying entities, adding new tables, or scaffolding CRUD features.
allowed-tools: Read, Grep, Glob, Bash, Write, Edit, Task
---

# .NET Clean Architecture — Entity Implementation Guide

You are an expert assistant that helps developers create or modify entities following the exact architecture patterns of this mqMonitor project. You guide the user through ALL required layers.

## Input

The user will describe the entity to create or modify: `$ARGUMENTS`

Before generating code, read existing files (use ProcessExecution as primary reference) to match current patterns exactly.

---

## Architecture & Data Flow

```
Controller → Service (interface) → Repository (interface) → DbContext → PostgreSQL
```

**Mapping chain:** EF Entity ↔ Domain Model ↔ DTO (two AutoMapper profiles per entity)

**Projects:**
- `MqMonitor.DTO` — Public API contracts (DTOs)
- `MqMonitor.Domain` — Entity interfaces, models, enums, service interfaces, messaging interfaces
- `MqMonitor.Infra.Interfaces` — Repository contracts
- `MqMonitor.Infra` — EF Core entities, DbContext, repositories, AutoMapper profiles, RabbitMQ, service implementations
- `MqMonitor.Application` — DI registration (Initializer.cs)
- `MqMonitor.API` — Controllers, Consumers

**Dependency graph:**
```
DTO (no deps)
Infra.Interfaces → Domain
Domain → DTO
Infra → Domain, Infra.Interfaces, DTO
Application → Domain, Infra, Infra.Interfaces
API/Worker/Producer → Application, Domain, DTO, Infra
```

---

## Step-by-Step Implementation

### Step 1: DTO — `MqMonitor.DTO/{Entity}Info.cs`

```csharp
namespace MqMonitor.DTO;

public class {Entity}Info
{
    public string {Entity}Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    // Nullable types for optional fields (DateTime?, string?)
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

Convention: public setters, DTOs are data bags. Use `= string.Empty` for non-nullable strings.

### Step 2: Domain Interface — `MqMonitor.Domain/Entities/Interfaces/I{Entity}Model.cs`

```csharp
namespace MqMonitor.Domain.Entities.Interfaces;

public interface I{Entity}Model
{
    string {Entity}Id { get; }
    string Name { get; }
    DateTime CreatedAt { get; }
    DateTime UpdatedAt { get; }
    // Read-only properties only
}
```

### Step 3: Domain Model — `MqMonitor.Domain/Entities/{Entity}Model.cs`

Key patterns (see `TestExecutionModel.cs` as reference):
- **Private setters** on all properties
- **Private parameterless constructor** (for mapper)
- **Factory methods:** `Create(...)` for new, `Reconstruct(...)` from persistence
- **Validation** in factory methods
- **`Equals`/`GetHashCode`** by Id

```csharp
using MqMonitor.Domain.Entities.Interfaces;

namespace MqMonitor.Domain.Entities;

public class {Entity}Model : I{Entity}Model
{
    public string {Entity}Id { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private {Entity}Model() { }

    public static {Entity}Model Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        return new {Entity}Model
        {
            {Entity}Id = Guid.NewGuid().ToString(),
            Name = name.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static {Entity}Model Reconstruct(string id, string name,
        DateTime createdAt, DateTime updatedAt)
    {
        return new {Entity}Model
        {
            {Entity}Id = id,
            Name = name,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public void Update(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public override bool Equals(object? obj) =>
        obj is {Entity}Model other && {Entity}Id == other.{Entity}Id;
    public override int GetHashCode() => {Entity}Id.GetHashCode();
}
```

### Step 4: EF Entity — `MqMonitor.Infra/Context/{Entity}.cs`

```csharp
namespace MqMonitor.Infra.Context;

public partial class {Entity}
{
    public string {Entity}Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

Convention: `partial class`, public setters, `= string.Empty` for non-nullable strings.

### Step 5: DbContext — Modify `MqMonitor.Infra/Context/MonitorDbContext.cs`

Add DbSet and configure in `OnModelCreating`:

```csharp
// Add DbSet
public DbSet<{Entity}> {Entity}s => Set<{Entity}>();

// Inside OnModelCreating:
modelBuilder.Entity<{Entity}>(entity =>
{
    entity.ToTable("{entities}");  // snake_case plural
    entity.HasKey(e => e.{Entity}Id);

    entity.Property(e => e.{Entity}Id).HasColumnName("{entity}_id").HasMaxLength(100);
    entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(240).IsRequired();
    entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
    entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();

    entity.HasIndex(e => e.Name);
});
```

Convention: snake_case table/columns, PostgreSQL `timestamp with time zone`.

### Step 6: Migration

```bash
dotnet ef migrations add Add{Entity}Table --project MqMonitor.Infra --startup-project MqMonitor.API
dotnet ef database update --project MqMonitor.Infra --startup-project MqMonitor.API
```

### Step 7: Repository Interface — `MqMonitor.Infra.Interfaces/Repository/I{Entity}Repository.cs`

```csharp
using MqMonitor.Domain.Entities.Interfaces;

namespace MqMonitor.Infra.Interfaces.Repository;

public interface I{Entity}Repository<TModel> where TModel : I{Entity}Model
{
    Task<IEnumerable<TModel>> GetAllAsync();
    Task<TModel?> GetByIdAsync(string id);
    Task<TModel> InsertAsync(TModel model);
    Task<TModel> UpdateAsync(TModel model);
    Task DeleteAsync(string id);
}
```

Convention: Generic `<TModel>` with constraint. Async methods. String keys.

### Step 8: Repository Implementation — `MqMonitor.Infra/Repository/{Entity}Repository.cs`

Key patterns (see `ProcessExecutionRepository.cs`):
- Inject `MonitorDbContext` + `IMapper`
- **Reads:** `AsNoTracking()`, map EF → Domain via `_mapper.Map<{Entity}Model>(...)`
- **Insert:** map model → EF entity, Add, SaveChangesAsync, map back
- **Update:** Fetch tracked entity, mutate properties, SaveChangesAsync
- **Delete:** Fetch, remove, SaveChangesAsync, throw `KeyNotFoundException` if not found

```csharp
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MqMonitor.Domain.Entities;
using MqMonitor.Domain.Entities.Interfaces;
using MqMonitor.Infra.Context;
using MqMonitor.Infra.Interfaces.Repository;

namespace MqMonitor.Infra.Repository;

public class {Entity}Repository : I{Entity}Repository<I{Entity}Model>
{
    private readonly MonitorDbContext _context;
    private readonly IMapper _mapper;

    public {Entity}Repository(MonitorDbContext context, IMapper mapper)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<IEnumerable<I{Entity}Model>> GetAllAsync()
    {
        var entities = await _context.{Entity}s.AsNoTracking()
            .OrderBy(e => e.Name).ToListAsync();
        return _mapper.Map<IEnumerable<{Entity}Model>>(entities);
    }

    public async Task<I{Entity}Model?> GetByIdAsync(string id)
    {
        var entity = await _context.{Entity}s.AsNoTracking()
            .FirstOrDefaultAsync(e => e.{Entity}Id == id);
        if (entity == null) return null;
        return _mapper.Map<{Entity}Model>(entity);
    }

    public async Task<I{Entity}Model> InsertAsync(I{Entity}Model model)
    {
        var entity = _mapper.Map<{Entity}>(model);
        _context.{Entity}s.Add(entity);
        await _context.SaveChangesAsync();
        return _mapper.Map<{Entity}Model>(entity);
    }

    public async Task<I{Entity}Model> UpdateAsync(I{Entity}Model model)
    {
        var existing = await _context.{Entity}s
            .FirstOrDefaultAsync(e => e.{Entity}Id == model.{Entity}Id)
            ?? throw new KeyNotFoundException($"{Entity} with ID {model.{Entity}Id} not found.");
        existing.Name = model.Name;
        existing.UpdatedAt = model.UpdatedAt;
        await _context.SaveChangesAsync();
        return _mapper.Map<{Entity}Model>(existing);
    }

    public async Task DeleteAsync(string id)
    {
        var entity = await _context.{Entity}s
            .FirstOrDefaultAsync(e => e.{Entity}Id == id)
            ?? throw new KeyNotFoundException($"{Entity} with ID {id} not found.");
        _context.{Entity}s.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
```

### Step 9: AutoMapper Profiles — `MqMonitor.Infra/Mapping/Profiles/`

**Two profiles per entity:**

**`{Entity}Profile.cs`** (EF Entity ↔ Domain Model):
```csharp
using AutoMapper;
using MqMonitor.Domain.Entities;
using MqMonitor.Infra.Context;

namespace MqMonitor.Infra.Mapping.Profiles;

public class {Entity}Profile : Profile
{
    public {Entity}Profile()
    {
        CreateMap<{Entity}, {Entity}Model>()
            .ConstructUsing(src => {Entity}Model.Reconstruct(
                src.{Entity}Id, src.Name, src.CreatedAt, src.UpdatedAt));

        CreateMap<{Entity}Model, {Entity}>();
    }
}
```

**`{Entity}DtoProfile.cs`** (Domain Model ↔ DTO):
```csharp
using AutoMapper;
using MqMonitor.Domain.Entities;
using MqMonitor.Domain.Entities.Interfaces;
using MqMonitor.DTO;

namespace MqMonitor.Infra.Mapping.Profiles;

public class {Entity}DtoProfile : Profile
{
    public {Entity}DtoProfile()
    {
        CreateMap<{Entity}Model, {Entity}Info>();
        CreateMap<I{Entity}Model, {Entity}Info>();
    }
}
```

Convention: `ConstructUsing` with `Reconstruct()` factory. Map both concrete and interface.

### Step 10: Service Interface — `MqMonitor.Domain/Services/Interfaces/I{Entity}Service.cs`

```csharp
using MqMonitor.DTO;

namespace MqMonitor.Domain.Services.Interfaces;

public interface I{Entity}Service
{
    Task<List<{Entity}Info>> GetAllAsync();
    Task<{Entity}Info?> GetByIdAsync(string id);
    Task<{Entity}Info> InsertAsync({Entity}Info dto);
    Task<{Entity}Info> UpdateAsync({Entity}Info dto);
    Task DeleteAsync(string id);
}
```

Convention: Services receive/return **DTOs**, not domain models. Async methods.

### Step 11: Service Implementation — `MqMonitor.Infra/Services/{Entity}Service.cs`

Key patterns (see `ProcessQueryService.cs`):
- Inject repository (`I{Entity}Repository<I{Entity}Model>`) + `IMapper`
- Map: DTO → Domain Model → Repository → Domain Model → DTO
- Validation in service, not repository

```csharp
using AutoMapper;
using MqMonitor.Domain.Entities;
using MqMonitor.Domain.Entities.Interfaces;
using MqMonitor.Domain.Services.Interfaces;
using MqMonitor.DTO;
using MqMonitor.Infra.Interfaces.Repository;

namespace MqMonitor.Infra.Services;

public class {Entity}Service : I{Entity}Service
{
    private readonly I{Entity}Repository<I{Entity}Model> _repository;
    private readonly IMapper _mapper;

    public {Entity}Service(
        I{Entity}Repository<I{Entity}Model> repository, IMapper mapper)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<List<{Entity}Info>> GetAllAsync()
    {
        var models = await _repository.GetAllAsync();
        return _mapper.Map<List<{Entity}Info>>(models);
    }

    public async Task<{Entity}Info?> GetByIdAsync(string id)
    {
        var model = await _repository.GetByIdAsync(id);
        if (model == null) return null;
        return _mapper.Map<{Entity}Info>(model);
    }

    public async Task<{Entity}Info> InsertAsync({Entity}Info dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));
        var model = {Entity}Model.Create(dto.Name);
        var result = await _repository.InsertAsync(model);
        return _mapper.Map<{Entity}Info>(result);
    }

    public async Task<{Entity}Info> UpdateAsync({Entity}Info dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));
        var existing = await _repository.GetByIdAsync(dto.{Entity}Id)
            ?? throw new KeyNotFoundException($"{Entity} not found.");
        var model = {Entity}Model.Reconstruct(
            existing.{Entity}Id, existing.Name,
            existing.CreatedAt, existing.UpdatedAt);
        model.Update(dto.Name);
        var result = await _repository.UpdateAsync(model);
        return _mapper.Map<{Entity}Info>(result);
    }

    public async Task DeleteAsync(string id) => await _repository.DeleteAsync(id);
}
```

### Step 12: DI Registration — Modify `MqMonitor.Application/Initializer.cs`

Add entries in `AddMqMonitor()`:

```csharp
// Repository:
services.AddScoped<I{Entity}Repository<I{Entity}Model>, {Entity}Repository>();

// Service:
services.AddScoped<I{Entity}Service, {Entity}Service>();

// AutoMapper profiles are auto-discovered by assembly scan (already configured)
```

### Step 13: Controller — `MqMonitor.API/Controllers/{Entity}Controller.cs`

Key patterns (see `ProcessesController.cs`):
- Inject `I{Entity}Service`, `ILogger`
- Error handling: `KeyNotFoundException` → 404, `ArgumentException` → 400, generic → 500
- `CreatedAtAction` for POST responses

```csharp
using Microsoft.AspNetCore.Mvc;
using MqMonitor.Domain.Services.Interfaces;
using MqMonitor.DTO;

namespace MqMonitor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class {Entity}Controller : ControllerBase
{
    private readonly I{Entity}Service _service;
    private readonly ILogger<{Entity}Controller> _logger;

    public {Entity}Controller(I{Entity}Service service, ILogger<{Entity}Controller> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<{Entity}Info>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof({Entity}Info), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof({Entity}Info), StatusCodes.Status201Created)]
    public async Task<IActionResult> Insert([FromBody] {Entity}Info dto)
    {
        var result = await _service.InsertAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.{Entity}Id }, result);
    }

    [HttpPut]
    [ProducesResponseType(typeof({Entity}Info), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update([FromBody] {Entity}Info dto)
    {
        var result = await _service.UpdateAsync(dto);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
```

---

## Checklist

| # | Layer | Action | File |
|---|-------|--------|------|
| 1 | DTO | Create | `MqMonitor.DTO/{Entity}Info.cs` |
| 2 | Domain | Create | `MqMonitor.Domain/Entities/Interfaces/I{Entity}Model.cs` |
| 3 | Domain | Create | `MqMonitor.Domain/Entities/{Entity}Model.cs` |
| 4 | Infra | Create | `MqMonitor.Infra/Context/{Entity}.cs` |
| 5 | Infra | Modify | `MqMonitor.Infra/Context/MonitorDbContext.cs` |
| 6 | Infra | Run | `dotnet ef migrations add Add{Entity}Table` |
| 7 | Infra.Interfaces | Create | `MqMonitor.Infra.Interfaces/Repository/I{Entity}Repository.cs` |
| 8 | Infra | Create | `MqMonitor.Infra/Repository/{Entity}Repository.cs` |
| 9 | Infra | Create | `MqMonitor.Infra/Mapping/Profiles/{Entity}Profile.cs` |
| 10 | Infra | Create | `MqMonitor.Infra/Mapping/Profiles/{Entity}DtoProfile.cs` |
| 11 | Domain | Create | `MqMonitor.Domain/Services/Interfaces/I{Entity}Service.cs` |
| 12 | Infra | Create | `MqMonitor.Infra/Services/{Entity}Service.cs` |
| 13 | Application | Modify | `MqMonitor.Application/Initializer.cs` (2 registrations) |
| 14 | API | Create | `MqMonitor.API/Controllers/{Entity}Controller.cs` |

## Response Guidelines

1. **Read existing files first** to match current patterns exactly
2. **Follow the order** — DTO → Domain → Infra → Application → API
3. **Use ProcessExecution** as primary reference (complete example in this project)
4. **Run migrations** after modifying DbContext
5. **Match conventions**: snake_case DB, PascalCase C#, factory methods, private setters
6. **PostgreSQL**: `timestamp with time zone`, string PKs, `HasMaxLength` for strings
7. **Async everywhere**: all repository and service methods are async (Task<>)
