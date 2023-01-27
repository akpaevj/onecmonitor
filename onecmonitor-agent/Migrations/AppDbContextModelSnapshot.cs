﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OnecMonitor.Agent;

#nullable disable

namespace OnecMonitor.Agent.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.2");

            modelBuilder.Entity("OnecMonitor.Agent.Models.TechLogSeance", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("FinishDateTime")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("StartDateTime")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("Status")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Template")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("TechLogSeances");
                });

            modelBuilder.Entity("OnecMonitor.Common.Models.AgentInstance", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("InstanceName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<double>("UtcOffset")
                        .HasColumnType("REAL");

                    b.HasKey("Id");

                    b.ToTable("AgentInstance");
                });
#pragma warning restore 612, 618
        }
    }
}
