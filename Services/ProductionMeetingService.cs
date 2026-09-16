using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ProductionMeeting.Data;
using ProductionMeeting.Models;
using ProductionMeeting.ViewModels.ProductionMeeting;

namespace ProductionMeeting.Services;

public class ProductionMeetingService : IProductionMeetingService
{
    private readonly ApplicationDbContext _db;
    private readonly IUserAccessService _accessService;

    public ProductionMeetingService(ApplicationDbContext db, IUserAccessService accessService)
    {
        _db = db;
        _accessService = accessService;
    }

    public async Task<MeetingIndexViewModel> GetIndexAsync(MeetingIndexViewModel filters, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var accessiblePlants = await _accessService.GetAccessiblePlantsAsync(userId, isAdmin, cancellationToken);
        var accessiblePlantIds = accessiblePlants.Select(p => p.PlantId).ToList();

        filters.Plants = accessiblePlants.Select(p => (p.PlantId, p.Name)).ToList();
        filters.Shifts = await _db.Shifts.Where(s => s.IsActive).OrderBy(s => s.Name)
            .Select(s => new ValueTuple<int, string>(s.ShiftId, s.Name)).ToListAsync(cancellationToken);

        if (filters.PlantId.HasValue)
        {
            var lines = await _accessService.GetAccessibleLinesAsync(userId, isAdmin, filters.PlantId.Value, cancellationToken);
            filters.Lines = lines.Select(l => (l.LineId, l.Name)).ToList();
        }

        var query = _db.MeetingSessions
            .Include(s => s.Plant)
            .Include(s => s.Line)
            .Include(s => s.Shift)
            .Include(s => s.ConductedBy)
            .Include(s => s.Transactions)
            .Where(s => accessiblePlantIds.Contains(s.PlantId))
            .AsQueryable();

        if (filters.PlantId.HasValue)
        {
            query = query.Where(s => s.PlantId == filters.PlantId.Value);
        }
        if (filters.LineId.HasValue)
        {
            query = query.Where(s => s.LineId == filters.LineId.Value);
        }
        if (filters.FromDate.HasValue)
        {
            query = query.Where(s => s.MeetingDate >= filters.FromDate.Value.Date);
        }
        if (filters.ToDate.HasValue)
        {
            query = query.Where(s => s.MeetingDate <= filters.ToDate.Value.Date);
        }

        filters.TotalCount = await query.CountAsync(cancellationToken);

        var page = Math.Max(1, filters.Page);
        var pageSize = filters.PageSize is > 0 and <= 100 ? filters.PageSize : 10;

        var sessions = await query
            .OrderByDescending(s => s.MeetingDate)
            .ThenByDescending(s => s.SessionId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var kpiCounts = await _db.KpiMasters
            .Where(k => k.IsActive)
            .GroupBy(k => new { k.PlantId, k.LineId })
            .Select(g => new { g.Key.PlantId, g.Key.LineId, Count = g.Count() })
            .ToListAsync(cancellationToken);

        filters.Sessions = sessions.Select(s => new MeetingListItemViewModel
        {
            SessionId = s.SessionId,
            PlantName = s.Plant.Name,
            LineName = s.Line.Name,
            ShiftName = s.Shift?.Name,
            MeetingDate = s.MeetingDate,
            Status = s.Status == MeetingSessionStatus.Completed ? "Completed" : "Draft",
            ConductedByName = s.ConductedBy.FullName,
            KpiCount = kpiCounts.FirstOrDefault(k => k.PlantId == s.PlantId && k.LineId == s.LineId)?.Count ?? 0,
            EnteredCount = s.Transactions.Count
        }).ToList();

        filters.Page = page;
        filters.PageSize = pageSize;

        return filters;
    }

    public async Task<MeetingEntryViewModel?> GetOrCreateEntryAsync(int plantId, int lineId, int? shiftId, DateTime meetingDate, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (!await _accessService.CanAccessLineAsync(userId, isAdmin, plantId, lineId, cancellationToken))
        {
            return null;
        }

        var date = meetingDate.Date;

        var session = await _db.MeetingSessions
            .FirstOrDefaultAsync(s => s.PlantId == plantId && s.LineId == lineId && s.ShiftId == shiftId && s.MeetingDate == date, cancellationToken);

        if (session == null)
        {
            session = new MeetingSession
            {
                PlantId = plantId,
                LineId = lineId,
                ShiftId = shiftId,
                MeetingDate = date,
                ConductedByUserId = userId,
                Status = MeetingSessionStatus.Draft,
                CreatedBy = userId
            };
            _db.MeetingSessions.Add(session);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return await BuildEntryViewModelAsync(session, readOnly: false, cancellationToken);
    }

    public async Task<MeetingEntryViewModel?> GetEntryBySessionIdAsync(int sessionId, string userId, bool isAdmin, bool readOnly, CancellationToken cancellationToken = default)
    {
        var session = await _db.MeetingSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken);
        if (session == null)
        {
            return null;
        }

        if (!await _accessService.CanAccessLineAsync(userId, isAdmin, session.PlantId, session.LineId, cancellationToken))
        {
            return null;
        }

        return await BuildEntryViewModelAsync(session, readOnly || session.Status == MeetingSessionStatus.Completed, cancellationToken);
    }

    private async Task<MeetingEntryViewModel> BuildEntryViewModelAsync(MeetingSession session, bool readOnly, CancellationToken cancellationToken)
    {
        var plant = await _db.Plants.FirstAsync(p => p.PlantId == session.PlantId, cancellationToken);
        var line = await _db.ProductionLines.FirstAsync(l => l.LineId == session.LineId, cancellationToken);

        var kpis = await _db.KpiMasters
            .Include(k => k.Indicator)
            .Include(k => k.Unit)
            .Where(k => k.IsActive && k.PlantId == session.PlantId && k.LineId == session.LineId)
            .OrderBy(k => k.Indicator.IndicatorId)
            .ThenBy(k => k.DisplayOrder)
            .ToListAsync(cancellationToken);

        var transactions = await _db.KpiTransactions
            .Where(t => t.SessionId == session.SessionId)
            .ToListAsync(cancellationToken);

        var models = await _db.ProductModels.Where(m => m.IsActive).OrderBy(m => m.Name)
            .Select(m => new ValueTuple<int, string>(m.ModelId, m.Name)).ToListAsync(cancellationToken);

        var groups = kpis
            .GroupBy(k => k.Indicator)
            .Select(g => new IndicatorGroupViewModel
            {
                IndicatorId = g.Key.IndicatorId,
                IndicatorCode = g.Key.Code,
                IndicatorName = g.Key.Name,
                Rows = g.Select(k =>
                {
                    var tx = transactions.FirstOrDefault(t => t.KpiId == k.KpiId);
                    return new KpiEntryRowViewModel
                    {
                        KpiId = k.KpiId,
                        Description = k.Description,
                        UnitName = k.Unit.Name,
                        DisplayOrder = k.DisplayOrder,
                        TransactionId = tx?.TransactionId,
                        ModelId = tx?.ModelId,
                        F26Value = tx?.F26Value,
                        F27Value = tx?.F27Value,
                        WeekValue = tx?.WeekValue,
                        MonthCumValue = tx?.MonthCumValue,
                        YtdValue = tx?.YtdValue,
                        Remarks = tx?.Remarks
                    };
                }).ToList()
            }).ToList();

        return new MeetingEntryViewModel
        {
            SessionId = session.SessionId,
            PlantId = session.PlantId,
            PlantName = plant.Name,
            LineId = session.LineId,
            LineName = line.Name,
            ShiftId = session.ShiftId,
            MeetingDate = session.MeetingDate,
            Status = session.Status == MeetingSessionStatus.Completed ? "Completed" : "Draft",
            Remarks = session.Remarks,
            ReadOnly = readOnly,
            Models = models,
            IndicatorGroups = groups
        };
    }

    public async Task<SaveEntryResult> SaveEntryAsync(SaveEntryRequest request, string userId, string userFullName, CancellationToken cancellationToken = default)
    {
        var session = await _db.MeetingSessions.FirstOrDefaultAsync(s => s.SessionId == request.SessionId, cancellationToken);
        if (session == null)
        {
            return new SaveEntryResult { Success = false, Message = "Meeting session not found." };
        }

        if (session.Status == MeetingSessionStatus.Completed)
        {
            return new SaveEntryResult { Success = false, Message = "This meeting has been completed and can no longer be edited." };
        }

        var existingTransactions = await _db.KpiTransactions
            .Where(t => t.SessionId == request.SessionId)
            .ToListAsync(cancellationToken);

        var auditsToAdd = new List<KpiTransactionAudit>();
        var now = DateTime.UtcNow;

        foreach (var row in request.Rows)
        {
            var existing = existingTransactions.FirstOrDefault(t => t.KpiId == row.KpiId && t.ModelId == row.ModelId);

            if (existing == null)
            {
                var transaction = new KpiTransaction
                {
                    SessionId = request.SessionId,
                    KpiId = row.KpiId,
                    ModelId = row.ModelId,
                    F26Value = row.F26Value,
                    F27Value = row.F27Value,
                    WeekValue = row.WeekValue,
                    MonthCumValue = row.MonthCumValue,
                    YtdValue = row.YtdValue,
                    Remarks = row.Remarks,
                    CreatedBy = userId,
                    CreatedDate = now
                };
                _db.KpiTransactions.Add(transaction);
                await _db.SaveChangesAsync(cancellationToken); // need TransactionId for the audit rows below

                LogIfPresent(auditsToAdd, transaction.TransactionId, row.KpiId, "F26_Value", row.F26Value, userId, now);
                LogIfPresent(auditsToAdd, transaction.TransactionId, row.KpiId, "F27_Value", row.F27Value, userId, now);
                LogIfPresent(auditsToAdd, transaction.TransactionId, row.KpiId, "WeekValue", row.WeekValue, userId, now);
                LogIfPresent(auditsToAdd, transaction.TransactionId, row.KpiId, "MonthCumValue", row.MonthCumValue, userId, now);
                LogIfPresent(auditsToAdd, transaction.TransactionId, row.KpiId, "YtdValue", row.YtdValue, userId, now);
                LogIfPresent(auditsToAdd, transaction.TransactionId, row.KpiId, "Remarks", row.Remarks, userId, now);
            }
            else
            {
                LogIfChanged(auditsToAdd, existing.TransactionId, row.KpiId, "F26_Value", existing.F26Value, row.F26Value, userId, now);
                LogIfChanged(auditsToAdd, existing.TransactionId, row.KpiId, "F27_Value", existing.F27Value, row.F27Value, userId, now);
                LogIfChanged(auditsToAdd, existing.TransactionId, row.KpiId, "WeekValue", existing.WeekValue, row.WeekValue, userId, now);
                LogIfChanged(auditsToAdd, existing.TransactionId, row.KpiId, "MonthCumValue", existing.MonthCumValue, row.MonthCumValue, userId, now);
                LogIfChanged(auditsToAdd, existing.TransactionId, row.KpiId, "YtdValue", existing.YtdValue, row.YtdValue, userId, now);
                LogIfChanged(auditsToAdd, existing.TransactionId, row.KpiId, "Remarks", existing.Remarks, row.Remarks, userId, now);

                existing.F26Value = row.F26Value;
                existing.F27Value = row.F27Value;
                existing.WeekValue = row.WeekValue;
                existing.MonthCumValue = row.MonthCumValue;
                existing.YtdValue = row.YtdValue;
                existing.Remarks = row.Remarks;
                existing.ModifiedBy = userId;
                existing.ModifiedDate = now;
            }
        }

        if (request.MarkCompleted)
        {
            session.Status = MeetingSessionStatus.Completed;
            session.ModifiedBy = userId;
            session.ModifiedDate = now;
        }

        if (auditsToAdd.Count > 0)
        {
            _db.KpiTransactionAudits.AddRange(auditsToAdd);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new SaveEntryResult { Success = true, Message = "Saved successfully." };
    }

    public async Task<List<KpiTransactionAudit>> GetSessionAuditHistoryAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        return await _db.KpiTransactionAudits
            .Include(a => a.ChangedBy)
            .Include(a => a.Transaction)
            .Where(a => a.Transaction.SessionId == sessionId)
            .OrderByDescending(a => a.ChangedDate)
            .ToListAsync(cancellationToken);
    }

    private static string? ToAuditString(decimal? value) => value?.ToString(CultureInfo.InvariantCulture);

    private static void LogIfPresent(List<KpiTransactionAudit> audits, int transactionId, int kpiId, string field, decimal? value, string userId, DateTime now)
        => LogIfPresent(audits, transactionId, kpiId, field, ToAuditString(value), userId, now);

    private static void LogIfPresent(List<KpiTransactionAudit> audits, int transactionId, int kpiId, string field, string? value, string userId, DateTime now)
    {
        if (string.IsNullOrEmpty(value)) return;

        audits.Add(new KpiTransactionAudit
        {
            TransactionId = transactionId,
            KpiId = kpiId,
            FieldName = field,
            OldValue = null,
            NewValue = value,
            ActionType = "INSERT",
            ChangedByUserId = userId,
            ChangedDate = now
        });
    }

    private static void LogIfChanged(List<KpiTransactionAudit> audits, int transactionId, int kpiId, string field, decimal? oldValue, decimal? newValue, string userId, DateTime now)
    {
        // Compare as decimals first: 1m and 1.00m are equal in value but format to different
        // strings ("1" vs "1.00"), which would otherwise be misreported as a change.
        if (oldValue == newValue) return;
        LogIfChanged(audits, transactionId, kpiId, field, ToAuditString(oldValue), ToAuditString(newValue), userId, now);
    }

    private static void LogIfChanged(List<KpiTransactionAudit> audits, int transactionId, int kpiId, string field, string? oldValue, string? newValue, string userId, DateTime now)
    {
        if ((oldValue ?? string.Empty) == (newValue ?? string.Empty)) return;

        audits.Add(new KpiTransactionAudit
        {
            TransactionId = transactionId,
            KpiId = kpiId,
            FieldName = field,
            OldValue = oldValue,
            NewValue = newValue,
            ActionType = "UPDATE",
            ChangedByUserId = userId,
            ChangedDate = now
        });
    }
}
