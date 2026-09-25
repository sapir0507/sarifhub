using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SarifHub.Domain.Common;
using SarifHub.Domain.Findings;
using SarifHub.Domain.Gate;
using SarifHub.Domain.Projects;
using SarifHub.Domain.Rules;
using SarifHub.Domain.Scans;
using SarifHub.Domain.Triage;
using SarifHub.Domain.Users;
using SarifHub.Infrastructure.Persistence;

namespace SarifHub.Infrastructure.Seeding;

/// <summary>
/// Fills an empty database with four demo projects and their scan history, for local development and screenshots.
/// The history is built through the domain model — <see cref="Finding.Detect"/>, <see cref="Finding.SeenIn"/>,
/// <see cref="Finding.ResolvedIn"/>, <see cref="Scan.Complete"/>, <see cref="QualityGateEvaluator"/> — so every
/// stored count, lifecycle and gate result is consistent with the occurrence and resolution rows.
/// A fixed random seed per project keeps the data identical between runs; dates are relative to "now" so accepted
/// risks expire at meaningful times. Seeded scans have no raw SARIF artifact (Phase 4 adds real fixtures).
/// </summary>
public sealed partial class DevelopmentDataSeeder(SarifHubDbContext db, TimeProvider timeProvider, ILogger<DevelopmentDataSeeder> logger)
{
    /// <summary>Email of the user the Development API acts as (see <c>appsettings.Development.json</c>).</summary>
    public const string DemoUserEmail = "demo@sarifhub.local";

    private static readonly TimeSpan ScanInterval = TimeSpan.FromHours(84); // two scans a week

    private static readonly string[] FalsePositiveReasons =
    [
        "Input is validated by the route constraint before it reaches this call.",
        "Test-only code path; excluded from the production build.",
        "Value comes from server configuration, not from the request.",
        "Sanitized by the shared HtmlSanitizer before rendering.",
    ];

    private static readonly string[] AcceptedRiskReasons =
    [
        "Legacy integration scheduled for replacement in Q1; compensating control: WAF rule 1042.",
        "Internal admin endpoint reachable only from the VPN.",
        "Vendor SDK requirement; tracked with the vendor.",
    ];

    private static readonly string[] ConfirmedReasons =
    [
        "Reproduced locally.",
        "Valid finding, fix planned for next sprint.",
        "Confirmed during code review.",
    ];

    /// <summary>Seeds the database if it is fully migrated and has no projects yet.</summary>
    public async Task<SeedResult> SeedAsync(CancellationToken cancellationToken)
    {
        if ((await db.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            LogMigrationsPending(logger);
            return SeedResult.MigrationsPending;
        }

        if (await db.Projects.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            LogAlreadySeeded(logger);
            return SeedResult.AlreadySeeded;
        }

        var now = timeProvider.GetUtcNow();
        var created = now.AddDays(-90);

        var demo = User.Create(DemoUserEmail, "Demo User", created);
        var dana = User.Create("dana.levi@sarifhub.local", "Dana Levi", created);
        var noam = User.Create("noam.cohen@sarifhub.local", "Noam Cohen", created);
        var yael = User.Create("yael.mizrahi@sarifhub.local", "Yael Mizrahi", created);
        var amit = User.Create("amit.katz@sarifhub.local", "Amit Katz", created);
        db.Users.AddRange(demo, dana, noam, yael, amit);

        var rules = DemoRulePacks.Dotnet.Concat(DemoRulePacks.TypeScript)
            .DistinctBy(t => (t.Tool, t.RuleId))
            .ToDictionary(t => (t.Tool, t.RuleId), t => Rule.Create(t.Tool, t.RuleId, t.Name, t.Description, t.HelpUri, t.Cwe));
        db.Rules.AddRange(rules.Values);

        var specs = new ProjectSpec[]
        {
            new("payments-api", "Payments API", DemoRulePacks.Dotnet, ScanCount: 18, InstancesPerRule: (4, 14), new GatePolicy(0, 10, null), Seed: 42,
                Creator: demo, Members: [(dana, ProjectRole.SecurityLead), (noam, ProjectRole.Developer), (amit, ProjectRole.Developer), (yael, ProjectRole.Admin)]),
            new("customer-portal", "Customer Portal", DemoRulePacks.TypeScript, ScanCount: 11, InstancesPerRule: (2, 9), new GatePolicy(0, 5, null), Seed: 7,
                Creator: yael, Members: [(demo, ProjectRole.Developer), (dana, ProjectRole.SecurityLead), (amit, ProjectRole.Developer)]),
            new("map-tiles-service", "Map Tiles Service", [.. DemoRulePacks.TypeScript.Where(r => r.Files.Any(f => f.StartsWith("server/", StringComparison.Ordinal)))],
                ScanCount: 7, InstancesPerRule: (1, 4), new GatePolicy(0, 15, null), Seed: 99,
                Creator: yael, Members: [(demo, ProjectRole.SecurityLead), (noam, ProjectRole.Developer)]),

            // No scans yet: exercises the empty states.
            new("legacy-admin", "Legacy Admin", [], ScanCount: 0, InstancesPerRule: (0, 0), new GatePolicy(0, 10, null), Seed: 1,
                Creator: yael, Members: [(demo, ProjectRole.Viewer), (amit, ProjectRole.Developer)]),
        };

        var users = new[] { demo, dana, noam, yael, amit }.ToDictionary(u => u.Id);
        foreach (var spec in specs)
        {
            SeedProject(spec, rules, users, created, now);
        }

        var findingCount = db.ChangeTracker.Entries<Finding>().Count();
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogSeeded(logger, specs.Length, findingCount);
        return SeedResult.Seeded;
    }

    private void SeedProject(
        ProjectSpec spec,
        Dictionary<(string Tool, string RuleId), Rule> rules,
        Dictionary<Guid, User> users,
        DateTimeOffset created,
        DateTimeOffset now)
    {
        // Deterministic demo data, not security: System.Random with a fixed seed is the right tool here.
#pragma warning disable CA5394
        var rng = new Random(spec.Seed);

        var project = Project.Create(spec.Key, spec.Name, new Uri($"https://github.com/example-org/{spec.Key}"), "main", spec.GatePolicy, spec.Creator.Id, created);
        foreach (var (user, role) in spec.Members)
        {
            project.AddMember(user.Id, role, created.AddDays(1));
        }

        db.Projects.Add(project);
        if (spec.ScanCount == 0)
        {
            return;
        }

        var ciKey = ApiKey.Create(
            project.Id,
            "ci-main",
            RandomPrefix(),
            SHA256.HashData(RandomNumberGenerator.GetBytes(32)), // the secret is generated, hashed and discarded
            ApiKeyScopes.All,
            spec.Creator.Id,
            created.AddDays(1));
        db.ApiKeys.Add(ciKey);

        // Candidate pool: every place a rule could fire during the repository's history.
        var candidates = new List<Candidate>();
        foreach (var template in spec.Rules)
        {
            var count = spec.InstancesPerRule.Min + rng.Next(spec.InstancesPerRule.Max - spec.InstancesPerRule.Min + 1);
            for (var i = 0; i < count; i++)
            {
                var file = template.Files[rng.Next(template.Files.Count)];
                var fingerprint = "v1:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"{spec.Key}|{template.RuleId}|{file}|{i}")));
                candidates.Add(new Candidate(template, rules[(template.Tool, template.RuleId)], file, fingerprint, 20 + rng.Next(380)));
            }
        }

        var uploaders = spec.Members.Where(m => m.Role >= ProjectRole.Developer).Select(m => m.User.Id).Append(spec.Creator.Id).ToArray();
        var pendingDecisions = new List<TriageDecision>();
        // The latest scan lands a few hours before "now", like a nightly CI run.
        var firstScanAt = now - (ScanInterval * (spec.ScanCount - 1)) - TimeSpan.FromHours(10);

        for (var n = 1; n <= spec.ScanCount; n++)
        {
            var progress = (double)n / spec.ScanCount;
            var uploadedAt = firstScanAt + (ScanInterval * (n - 1)) + TimeSpan.FromMinutes(rng.Next(6 * 60));
            ApplyDecisionsDueBy(uploadedAt, pendingDecisions, candidates);

            var uploader = rng.NextDouble() < 0.85 ? ScanUploader.ApiKey(ciKey.Id) : ScanUploader.User(uploaders[rng.Next(uploaders.Length)]);
            var commit = RandomHex(rng, 40);
            var scan = Scan.Start(project, "main", commit, SHA256.HashData(Encoding.UTF8.GetBytes($"{spec.Key}/{n}/{commit}")), uploader, uploadedAt);

            int newCount = 0, existing = 0, reopened = 0, resolved = 0;
            var present = new List<Candidate>();
            foreach (var c in candidates)
            {
                var wasPresent = c.Finding is { Lifecycle: not Lifecycle.Resolved };
                var isPresent = n == 1 ? rng.NextDouble() < 0.55
                    : wasPresent ? rng.NextDouble() > 0.05 + (progress * 0.08) // the team works the backlog
                    : c.Finding is not null ? rng.NextDouble() < 0.07 // regression
                    : rng.NextDouble() < (n == spec.ScanCount ? 0.12 : 0.07 - (progress * 0.04)); // newly introduced

                if (isPresent)
                {
                    // Lines drift as surrounding code changes; the fingerprint does not depend on them.
                    c.Line = Math.Max(1, c.Line + (rng.Next(3) == 0 ? rng.Next(7) - 2 : 0));
                    var location = new FindingLocation(c.File, c.Line, 9, c.Line, c.Template.Message, c.Template.FlaggedLine);
                    if (c.Finding is null)
                    {
                        c.Finding = Finding.Detect(project.Id, c.Rule.Id, c.Fingerprint, c.Template.Severity, scan, location, PartialFingerprints(c, rng));
                        db.Findings.Add(c.Finding);
                        newCount++;
                        pendingDecisions.AddRange(PlanTriage(c.Finding, spec, users, rng, now));
                    }
                    else if (c.Finding.SeenIn(scan, c.Template.Severity, location, PartialFingerprints(c, rng)) == Lifecycle.Existing)
                    {
                        existing++;
                    }
                    else
                    {
                        reopened++;
                    }

                    present.Add(c);
                }
                else if (wasPresent)
                {
                    c.Finding!.ResolvedIn(scan);
                    resolved++;
                }
            }

            foreach (var tool in present.GroupBy(c => c.Template.Tool).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                scan.AddTool(tool.Key, tool.Key == "CodeQL" ? "2.23.1" : "1.139.0", tool.Count());
            }

            var active = SeverityCounts.Of(present.Where(c => !c.Finding!.IsSuppressed(uploadedAt)).Select(c => c.Template.Severity));
            scan.Complete(
                new ScanCounts(present.Count, newCount, existing, reopened, resolved),
                SeverityCounts.Of(present.Select(c => c.Template.Severity)),
                QualityGateEvaluator.Evaluate(active, project.GatePolicy));
            db.Scans.Add(scan);
        }

        // Decisions made since the last scan (never in the future).
        ApplyDecisionsDueBy(now.AddHours(-1), pendingDecisions, candidates);
#pragma warning restore CA5394
    }

    /// <summary>
    /// Plans how the team triaged a new finding: about 30% confirmed, 10% false positives, 7% confirmed and then
    /// accepted as a risk (some acceptances expire soon, one set has lapsed), the rest untriaged.
    /// The decider is always a member whose role allows the decision (<see cref="TriagePolicy"/>).
    /// </summary>
    private static IEnumerable<TriageDecision> PlanTriage(Finding finding, ProjectSpec spec, Dictionary<Guid, User> users, Random rng, DateTimeOffset now)
    {
#pragma warning disable CA5394
        var roll = rng.NextDouble();
        var at = Min(finding.FirstSeenAt + TimeSpan.FromHours(12 + rng.Next(96)), now.AddHours(-2));

        Guid Decider(TriageStatus status)
        {
            var allowed = spec.Members.Append((User: users[spec.Creator.Id], Role: ProjectRole.Admin))
                .Where(m => TriagePolicy.CanDecide(m.Role, status))
                .Select(m => m.User.Id)
                .Distinct()
                .ToArray();
            return allowed[rng.Next(allowed.Length)];
        }

        if (roll < 0.10)
        {
            yield return TriageDecision.Create(finding.Id, TriageStatus.FalsePositive, FalsePositiveReasons[rng.Next(FalsePositiveReasons.Length)], null, Decider(TriageStatus.FalsePositive), at);
        }
        else if (roll < 0.17)
        {
            yield return TriageDecision.Create(finding.Id, TriageStatus.Confirmed, ConfirmedReasons[rng.Next(ConfirmedReasons.Length)], null, Decider(TriageStatus.Confirmed), at);
            var acceptedAt = Min(at.AddDays(2), now.AddHours(-1));
            int[] expiryDays = [-3, 6, 11, 45, 90, 120];
            var expires = now.AddDays(expiryDays[rng.Next(expiryDays.Length)]);
            if (expires <= acceptedAt)
            {
                expires = now.AddDays(45); // an acceptance cannot end before it is made
            }

            yield return TriageDecision.Create(finding.Id, TriageStatus.AcceptedRisk, AcceptedRiskReasons[rng.Next(AcceptedRiskReasons.Length)], expires, Decider(TriageStatus.AcceptedRisk), acceptedAt);
        }
        else if (roll < 0.47)
        {
            yield return TriageDecision.Create(finding.Id, TriageStatus.Confirmed, ConfirmedReasons[rng.Next(ConfirmedReasons.Length)], null, Decider(TriageStatus.Confirmed), at);
        }
#pragma warning restore CA5394
    }

    private static void ApplyDecisionsDueBy(DateTimeOffset time, List<TriageDecision> pending, List<Candidate> candidates)
    {
        var due = pending.Where(d => d.DecidedAt <= time).OrderBy(d => d.DecidedAt).ToList();
        foreach (var decision in due)
        {
            candidates.First(c => c.Finding?.Id == decision.FindingId).Finding!.Triage(decision);
            pending.Remove(decision);
        }
    }

    private static Dictionary<string, string>? PartialFingerprints(Candidate candidate, Random rng) =>
        candidate.Template.Tool == "CodeQL"
            ? new Dictionary<string, string>(StringComparer.Ordinal) { ["primaryLocationLineHash"] = RandomHex(rng, 16) + ":1" }
            : null;

    private static string RandomHex(Random rng, int length)
    {
#pragma warning disable CA5394
        var bytes = new byte[(length + 1) / 2];
        rng.NextBytes(bytes);
#pragma warning restore CA5394
        return Convert.ToHexStringLower(bytes)[..length];
    }

    private static string RandomPrefix() =>
        RandomNumberGenerator.GetString("abcdefghijklmnopqrstuvwxyz0123456789", 8);

    private static DateTimeOffset Min(DateTimeOffset a, DateTimeOffset b) => a < b ? a : b;

    [LoggerMessage(Level = LogLevel.Error, Message = "The database schema is not up to date. Run 'dotnet ef database update' first (see README).")]
    private static partial void LogMigrationsPending(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "The database already contains projects; the development seed was skipped.")]
    private static partial void LogAlreadySeeded(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded {ProjectCount} projects with {FindingCount} findings.")]
    private static partial void LogSeeded(ILogger logger, int projectCount, int findingCount);

    private sealed record ProjectSpec(
        string Key,
        string Name,
        IReadOnlyList<RuleTemplate> Rules,
        int ScanCount,
        (int Min, int Max) InstancesPerRule,
        GatePolicy GatePolicy,
        int Seed,
        User Creator,
        IReadOnlyList<(User User, ProjectRole Role)> Members);

    private sealed class Candidate(RuleTemplate template, Rule rule, string file, string fingerprint, int line)
    {
        public RuleTemplate Template { get; } = template;

        public Rule Rule { get; } = rule;

        public string File { get; } = file;

        public string Fingerprint { get; } = fingerprint;

        public int Line { get; set; } = line;

        public Finding? Finding { get; set; }
    }
}
