using AgentFree.API.Models;
using Microsoft.EntityFrameworkCore;

namespace AgentFree.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Agent> Agents => Set<Agent>();
        public DbSet<Session> Sessions => Set<Session>();
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<Tool> Tools => Set<Tool>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Agent 配置
            builder.Entity<Agent>()
                .HasIndex(a => a.Name)
                .IsUnique();

            // Session 配置
            builder.Entity<Session>()
                .HasOne<Agent>()
                .WithMany()
                .HasForeignKey(s => s.AgentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Message 配置
            builder.Entity<Message>()
                .HasOne<Session>()
                .WithMany()
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed data
            builder.Entity<Agent>().HasData(
                new Agent { Id = 1, Name = "hermes-coding", Description = "研发顾问，负责架构设计、代码走查", AgentId = "hermes-001", AgentType = "Hermes", BaseUrl = "http://127.0.0.1:5100", Status = "Active", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Agent { Id = 2, Name = "agent-coding", Description = "编码智能体，负责代码编写、功能开发", AgentId = "agent-001", AgentType = "Goldfish", BaseUrl = "http://127.0.0.1:5100", Status = "Inactive", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Agent { Id = 3, Name = "testing-bot", Description = "自动化测试智能体", AgentId = "test-001", AgentType = "Goldfish", BaseUrl = "http://127.0.0.1:5100", Status = "Inactive", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            );
        }
    }
}
