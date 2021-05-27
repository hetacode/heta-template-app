# Prerequisite
## Generate db models

```console
# Install EF CLI tools 
dotnet tool install --global dotnet-ef

# Generate model
dotnet ef dbcontext scaffold "host=localhost;database=repository-func-commits-history;username=postgres;password=postgrespass" Npgsql.EntityFrameworkCore.PostgreSQL -o Models -c RepositoryFuncDbContext
```

# Run function

## gitlib2 dependency
Before run function localy you should run below command:
```console
export LD_LIBRARY_PATH=$PWD/bin/output/runtimes/ubuntu.18.04-x64/native
```