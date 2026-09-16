using Microsoft.EntityFrameworkCore;
using ProductionMeeting.Models;

namespace ProductionMeeting.Data;

// Migrates the seed data proven in the source system's SQL script (Indicators, Units,
// Models, KPI Master) onto one default Plant + Line, so the existing dataset becomes
// the first working plant/line under the new generalized schema.
public static class SeedData
{
    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private const string SeedUser = "System";

    public static void Seed(ModelBuilder builder)
    {
        builder.Entity<Plant>().HasData(
            new Plant { PlantId = 1, Code = "MAH-XUV", Name = "Mahindra - XUV Plant", CreatedBy = SeedUser, CreatedDate = SeedDate }
        );

        builder.Entity<ProductionLine>().HasData(
            new ProductionLine { LineId = 1, PlantId = 1, Code = "TCF", Name = "XUV TCF Line", CreatedBy = SeedUser, CreatedDate = SeedDate }
        );

        builder.Entity<Shift>().HasData(
            new Shift { ShiftId = 1, Name = "General", CreatedBy = SeedUser, CreatedDate = SeedDate },
            new Shift { ShiftId = 2, Name = "Shift A", CreatedBy = SeedUser, CreatedDate = SeedDate },
            new Shift { ShiftId = 3, Name = "Shift B", CreatedBy = SeedUser, CreatedDate = SeedDate },
            new Shift { ShiftId = 4, Name = "Shift C", CreatedBy = SeedUser, CreatedDate = SeedDate }
        );

        builder.Entity<Department>().HasData(
            new Department { DepartmentId = 1, Name = "Production", CreatedBy = SeedUser, CreatedDate = SeedDate },
            new Department { DepartmentId = 2, Name = "Quality", CreatedBy = SeedUser, CreatedDate = SeedDate },
            new Department { DepartmentId = 3, Name = "Maintenance", CreatedBy = SeedUser, CreatedDate = SeedDate }
        );

        builder.Entity<Indicator>().HasData(
            new Indicator { IndicatorId = 1, Code = "S", Name = "Safety", CreatedBy = SeedUser, CreatedDate = SeedDate },
            new Indicator { IndicatorId = 2, Code = "Q", Name = "Quality", CreatedBy = SeedUser, CreatedDate = SeedDate },
            new Indicator { IndicatorId = 3, Code = "C", Name = "Cost", CreatedBy = SeedUser, CreatedDate = SeedDate },
            new Indicator { IndicatorId = 4, Code = "D", Name = "Delivery", CreatedBy = SeedUser, CreatedDate = SeedDate },
            new Indicator { IndicatorId = 5, Code = "P", Name = "Production", CreatedBy = SeedUser, CreatedDate = SeedDate },
            new Indicator { IndicatorId = 6, Code = "SUST", Name = "Sustainability", CreatedBy = SeedUser, CreatedDate = SeedDate },
            new Indicator { IndicatorId = 7, Code = "M", Name = "Morale", CreatedBy = SeedUser, CreatedDate = SeedDate },
            new Indicator { IndicatorId = 8, Code = "O", Name = "Others", CreatedBy = SeedUser, CreatedDate = SeedDate }
        );

        builder.Entity<Unit>().HasData(
            new Unit { UnitId = 1, Name = "Nos", CreatedBy = SeedUser, CreatedDate = SeedDate },
            new Unit { UnitId = 2, Name = "%", CreatedBy = SeedUser, CreatedDate = SeedDate },
            new Unit { UnitId = 3, Name = "Rs./Veh", CreatedBy = SeedUser, CreatedDate = SeedDate },
            new Unit { UnitId = 4, Name = "Ltrs/Veh", CreatedBy = SeedUser, CreatedDate = SeedDate },
            new Unit { UnitId = 5, Name = "Units/Veh", CreatedBy = SeedUser, CreatedDate = SeedDate },
            new Unit { UnitId = 6, Name = "Nos/Shift", CreatedBy = SeedUser, CreatedDate = SeedDate },
            new Unit { UnitId = 7, Name = "Rs. Lacs", CreatedBy = SeedUser, CreatedDate = SeedDate },
            new Unit { UnitId = 8, Name = "Nos/Month", CreatedBy = SeedUser, CreatedDate = SeedDate },
            new Unit { UnitId = 9, Name = "No. of Stages", CreatedBy = SeedUser, CreatedDate = SeedDate }
        );

        builder.Entity<ProductModel>().HasData(
            new ProductModel { ModelId = 1, Name = "XUV300", CreatedBy = SeedUser, CreatedDate = SeedDate },
            new ProductModel { ModelId = 2, Name = "XUV3XO", CreatedBy = SeedUser, CreatedDate = SeedDate },
            new ProductModel { ModelId = 3, Name = "XUV400", CreatedBy = SeedUser, CreatedDate = SeedDate }
        );

        builder.Entity<KpiMaster>().HasData(
            // Safety (Indicator 1)
            Kpi(1, 1, 1, "First Aid", 1),
            Kpi(2, 1, 1, "Near Miss incidence", 2),
            Kpi(3, 1, 1, "Fire Incidence", 3),

            // Quality (Indicator 2)
            Kpi(4, 2, 1, "Offline Rework RPT XUV3XO", 4),
            Kpi(5, 2, 1, "Offline Rework RPT XUV400", 5),
            Kpi(6, 2, 6, "Buyoff Manpower Deployed", 6),
            Kpi(7, 2, 6, "Rework Manpower Deployed", 7),
            Kpi(8, 2, 2, "Process wise Zero Defect Stages *", 8),

            // Cost (Indicator 3)
            Kpi(9, 3, 3, "Scrap Cost Process", 9),
            Kpi(10, 3, 7, "Repairs & Maint. Cost Saving", 10),
            Kpi(11, 3, 6, "Contract Labour", 11),
            Kpi(12, 3, 2, "Traceability", 12),

            // Delivery (Indicator 4)
            Kpi(13, 4, 2, "Schedule Adh.", 13),
            Kpi(14, 4, 1, "A Rank Breakdown", 14),
            Kpi(15, 4, 2, "Minor Stoppages", 15),
            Kpi(16, 4, 2, "Straight Pass Ratio", 16),
            Kpi(17, 4, 1, "Non RFD Veh > 5 days", 17),

            // Production (Indicator 5)
            Kpi(18, 5, 1, "Eq.veh/man/year", 18),

            // Sustainability (Indicator 6)
            Kpi(19, 6, 4, "Water", 19),
            Kpi(20, 6, 5, "Power", 20),

            // Morale (Indicator 7)
            Kpi(21, 7, 1, "Recognition of Associates through JIGYASA Portal", 21),
            Kpi(22, 7, 9, "Ergonomy Status", 22),

            // Others (Indicator 8)
            Kpi(23, 8, 1, "Highlights/Lowlights/Others", 23),
            Kpi(24, 8, 1, "CCTV Camera Working Status", 24)
        );
    }

    private static KpiMaster Kpi(int kpiId, int indicatorId, int unitId, string description, int displayOrder) => new()
    {
        KpiId = kpiId,
        PlantId = 1,
        LineId = 1,
        IndicatorId = indicatorId,
        UnitId = unitId,
        Description = description,
        DisplayOrder = displayOrder,
        CreatedBy = SeedUser,
        CreatedDate = SeedDate
    };
}
