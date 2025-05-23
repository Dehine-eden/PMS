﻿using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem1.Model.Entities;
using System.Reflection.Emit;

namespace ProjectManagementSystem1.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<UserAccessToken> UserAccessTokens { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectAssignment> ProjectAssignments { get; set; }

        public DbSet<ProjectTask> ProjectTasks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // ProjectTask relationships
            modelBuilder.Entity<ProjectTask>(entity =>
            {
                // Link to ProjectAssignment (required)
                entity.HasOne(t => t.ProjectAssignment)
                            .WithMany(pa => pa.Tasks) // Add this navigation property
                            .HasForeignKey(t => t.ProjectAssignmentId)
                            .OnDelete(DeleteBehavior.Restrict);

                // Self-referential hierarchy
                entity.HasOne(t => t.ParentTask)
                    .WithMany(t => t.SubTasks)
                    .HasForeignKey(t => t.ParentTaskId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Prevent circular references via SQL trigger (optional)
            //entity.HasCheckConstraint("CK_NoSelfReference", "ParentTaskId <> Id");

            //// Limit hierarchy depth
            //entity.HasCheckConstraint("CK_MaxDepth", "Depth BETWEEN 0 AND 10");

       

        }


        // If you have other tables like RefreshTokens:
        //public DbSet<RefreshToken> RefreshTokens { get; set; }
    }
}

// old code

//using Microsoft.EntityFrameworkCore;
//using ProjectManagementSystem1.Model.Entities;

//namespace ProjectManagementSystem1.Data
//{
//    public class AppDbContext : IdentityDbContext<ApplicationUser>
//    {
//        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

//        public DbSet<User> Users { get; set; }
//        public DbSet<RefreshToken> RefreshTokens { get; set; }

//    }
//}