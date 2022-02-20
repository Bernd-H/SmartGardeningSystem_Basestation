﻿// <auto-generated />
using System;
using GardeningSystem.DataAccess.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GardeningSystem.DataAccess.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    partial class DatabaseContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 64)
                .HasAnnotation("ProductVersion", "5.0.13");

            modelBuilder.Entity("GardeningSystem.Common.Models.Entities.ModuleData", b =>
                {
                    b.Property<Guid>("uniqueDataPointId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<Guid>("Id")
                        .HasColumnType("char(36)");

                    b.Property<double>("SoilMoisture")
                        .HasColumnType("double");

                    b.Property<double>("Temperature")
                        .HasColumnType("double");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("Timestamp");

                    b.HasKey("uniqueDataPointId");

                    b.ToTable("sensordata");
                });
#pragma warning restore 612, 618
        }
    }
}
