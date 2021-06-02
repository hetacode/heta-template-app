using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace RepositoryProcessorFunc.Models
{
    public partial class RepositoryFuncDbContext : DbContext
    {
        public RepositoryFuncDbContext()
        {
        }

        public RepositoryFuncDbContext(DbContextOptions<RepositoryFuncDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Commit> Commits { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseNpgsql("host=localhost;database=repository-func-commits-history;username=postgres;password=postgrespass");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "en_US.utf8");

            modelBuilder.Entity<Commit>(entity =>
            {
                entity.HasKey(e => e.RepoHash)
                    .HasName("commits_pkey");

                entity.ToTable("commits");

                entity.Property(e => e.RepoHash)
                    .HasMaxLength(65)
                    .HasColumnName("repo_hash");

                entity.Property(e => e.CommitHash)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("commit_hash");

                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
