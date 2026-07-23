using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Abstractions;
using NotificationService.Application.RegistrationReminders;

namespace NotificationService.Infrastructure.Persistence;

public sealed class RegistrationReminderCandidateReader : IRegistrationReminderCandidateReader
{
    private const byte StudentRole = 2;
    private readonly NotificationDbContext _db;

    public RegistrationReminderCandidateReader(NotificationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RegistrationReminderCandidate>> ListAsync(
        DateOnly registrationEndsOn,
        CancellationToken cancellationToken = default)
    {
        var rows = await (
            from term in _db.Terms
            where term.RegistrationEndDate == registrationEndsOn
            from enrollment in _db.Enrollments
            where !enrollment.IsPassed
            join subject in _db.Subjects on enrollment.SubjectId equals subject.Id
            join user in _db.Users on enrollment.StudentId equals user.PersonId
            where subject.IsActive && user.IsActive && user.Role == StudentRole
            where !_db.Registrations.Any(registration =>
                registration.StudentId == enrollment.StudentId &&
                registration.SubjectId == enrollment.SubjectId &&
                registration.TermId == term.Id &&
                registration.IsActive)
            select new
            {
                UserId = user.Id,
                TermId = term.Id,
                TermName = term.Name,
                term.RegistrationEndDate,
                SubjectName = subject.Name
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => new
            {
                row.UserId,
                row.TermId,
                row.TermName,
                row.RegistrationEndDate
            })
            .Select(group => new RegistrationReminderCandidate(
                group.Key.UserId,
                group.Key.TermId,
                group.Key.TermName,
                group.Key.RegistrationEndDate,
                group.Select(row => row.SubjectName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .ToList()))
            .ToList();
    }
}
