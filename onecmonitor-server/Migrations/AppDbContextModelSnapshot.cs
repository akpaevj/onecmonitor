﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OnecMonitor.Server;

#nullable disable

namespace OnecMonitor.Server.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.11");

            modelBuilder.Entity("AgentTechLogSeance", b =>
                {
                    b.Property<string>("ConnectedAgentsId")
                        .HasColumnType("TEXT");

                    b.Property<string>("SeancesId")
                        .HasColumnType("TEXT");

                    b.HasKey("ConnectedAgentsId", "SeancesId");

                    b.HasIndex("SeancesId");

                    b.ToTable("AgentTechLogSeance");
                });

            modelBuilder.Entity("LogTemplateTechLogSeance", b =>
                {
                    b.Property<string>("ConnectedTemplatesId")
                        .HasColumnType("TEXT");

                    b.Property<string>("SeancesId")
                        .HasColumnType("TEXT");

                    b.HasKey("ConnectedTemplatesId", "SeancesId");

                    b.HasIndex("SeancesId");

                    b.ToTable("LogTemplateTechLogSeance");
                });

            modelBuilder.Entity("OnecMonitor.Server.Models.Agent", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("InstanceName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Agents");
                });

            modelBuilder.Entity("OnecMonitor.Server.Models.LogTemplate", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("LogTemplates");
                });

            modelBuilder.Entity("OnecMonitor.Server.Models.TechLogFilter", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Filter")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("TechLogFilters");
                });

            modelBuilder.Entity("OnecMonitor.Server.Models.TechLogSeance", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("DirectSending")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Duration")
                        .HasColumnType("INTEGER");

                    b.Property<string>("StartDateTime")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("StartMode")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("TechLogSeances");
                });

            modelBuilder.Entity("AgentTechLogSeance", b =>
                {
                    b.HasOne("OnecMonitor.Server.Models.Agent", null)
                        .WithMany()
                        .HasForeignKey("ConnectedAgentsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("OnecMonitor.Server.Models.TechLogSeance", null)
                        .WithMany()
                        .HasForeignKey("SeancesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("LogTemplateTechLogSeance", b =>
                {
                    b.HasOne("OnecMonitor.Server.Models.LogTemplate", null)
                        .WithMany()
                        .HasForeignKey("ConnectedTemplatesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("OnecMonitor.Server.Models.TechLogSeance", null)
                        .WithMany()
                        .HasForeignKey("SeancesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
