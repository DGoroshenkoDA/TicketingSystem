using Microsoft.EntityFrameworkCore;
using Ticketing.Data.Entities;

namespace Ticketing.Data;

public class TicketingDbContext : DbContext
{
    public TicketingDbContext(DbContextOptions<TicketingDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Epic> Epics => Set<Epic>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Names are mapped to snake_case via UseSnakeCaseNamingConvention() in DI,
        // so table/column names match the hand-written migration DDL.

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(u => u.EmailNormalized).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(r => r.UserId);
            e.HasOne(r => r.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Team>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(t => t.NameNormalized).IsUnique();
        });

        modelBuilder.Entity<Epic>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            // Alternate key required as the target of the composite FK from tickets.
            e.HasAlternateKey(x => new { x.Id, x.TeamId });
            e.HasIndex(x => x.TeamId);
            e.HasOne(x => x.Team)
                .WithMany(t => t.Epics)
                .HasForeignKey(x => x.TeamId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Ticket>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            e.ToTable(t =>
            {
                t.HasCheckConstraint("ck_tickets_type", "type IN ('bug','feature','fix')");
                t.HasCheckConstraint(
                    "ck_tickets_state",
                    "state IN ('new','ready_for_implementation','in_progress','ready_for_acceptance','done')");
            });

            e.HasIndex(x => new { x.TeamId, x.State, x.ModifiedAt });
            e.HasIndex(x => new { x.TeamId, x.EpicId });
            e.HasIndex(x => new { x.TeamId, x.Type });
            e.HasIndex(x => x.EpicId);

            e.HasOne(x => x.Team)
                .WithMany(t => t.Tickets)
                .HasForeignKey(x => x.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Creator)
                .WithMany()
                .HasForeignKey(x => x.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Composite FK enforces that the epic belongs to the ticket's team.
            e.HasOne(x => x.Epic)
                .WithMany(ep => ep.Tickets)
                .HasForeignKey(x => new { x.EpicId, x.TeamId })
                .HasPrincipalKey(ep => new { ep.Id, ep.TeamId })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Comment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => new { x.TicketId, x.CreatedAt });

            e.HasOne(x => x.Ticket)
                .WithMany(t => t.Comments)
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Author)
                .WithMany()
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
