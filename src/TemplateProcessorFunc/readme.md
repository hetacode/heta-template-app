# Prerequisite
## Generate db models

```console
# Install EF CLI tools 
dotnet tool install --global dotnet-ef

# Generate DTO
dotnet ef dbcontext scaffold "host=localhost;database=template-func-commits-history;username=postgres;password=postgrespass" Npgsql.EntityFrameworkCore.PostgreSQL -o Models -c TemplateFuncDbContext -f
```