using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SarifHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tool_name = table.Column<string>(type: "text", nullable: false),
                    rule_id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: true),
                    short_description = table.Column<string>(type: "text", nullable: true),
                    help_uri = table.Column<string>(type: "text", nullable: true),
                    cwe = table.Column<string[]>(type: "text[]", nullable: false, defaultValueSql: "'{}'::text[]")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    normalized_email = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.CheckConstraint("ck_users_display_name_length", "length(display_name) BETWEEN 1 AND 100");
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    repository_url = table.Column<string>(type: "text", nullable: true),
                    default_branch = table.Column<string>(type: "text", nullable: false, defaultValue: "main"),
                    last_scan_number = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    gate_max_critical = table.Column<int>(type: "integer", nullable: true),
                    gate_max_high = table.Column<int>(type: "integer", nullable: true),
                    gate_max_medium = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projects", x => x.id);
                    table.CheckConstraint("ck_projects_gate_max_critical", "gate_max_critical >= 0");
                    table.CheckConstraint("ck_projects_gate_max_high", "gate_max_high >= 0");
                    table.CheckConstraint("ck_projects_gate_max_medium", "gate_max_medium >= 0");
                    table.CheckConstraint("ck_projects_key_format", "key ~ '^[a-z0-9][a-z0-9-]{1,62}$'");
                    table.CheckConstraint("ck_projects_name_length", "length(name) BETWEEN 1 AND 100");
                    table.CheckConstraint("ck_projects_repository_url_https", "repository_url ~ '^https://'");
                    table.ForeignKey(
                        name: "fk_projects_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "api_keys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    prefix = table.Column<string>(type: "character(8)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    key_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    scopes = table.Column<string[]>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_api_keys", x => x.id);
                    table.CheckConstraint("ck_api_keys_key_hash_sha256", "octet_length(key_hash) = 32");
                    table.CheckConstraint("ck_api_keys_name_length", "length(name) BETWEEN 1 AND 60");
                    table.CheckConstraint("ck_api_keys_scopes", "scopes <@ ARRAY['scans:write', 'gate:read']::text[] AND cardinality(scopes) > 0");
                    table.ForeignKey(
                        name: "fk_api_keys_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_api_keys_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "audit_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_type = table.Column<string>(type: "text", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    project_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "text", nullable: false),
                    target_type = table.Column<string>(type: "text", nullable: true),
                    target_id = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    data = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_events", x => x.id);
                    table.CheckConstraint("ck_audit_events_actor_type", "actor_type IN ('User', 'ApiKey', 'System', 'Anonymous')");
                    table.ForeignKey(
                        name: "fk_audit_events_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "project_members",
                columns: table => new
                {
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_members", x => new { x.project_id, x.user_id });
                    table.CheckConstraint("ck_project_members_role", "role IN ('Viewer', 'Developer', 'SecurityLead', 'Admin')");
                    table.ForeignKey(
                        name: "fk_project_members_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_project_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    branch = table.Column<string>(type: "text", nullable: false),
                    commit_sha = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    uploaded_by_key_id = table.Column<Guid>(type: "uuid", nullable: true),
                    total_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    new_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    existing_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    reopened_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    resolved_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    critical_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    high_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    medium_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    low_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    gate_result = table.Column<string>(type: "text", nullable: true),
                    gate_evaluation = table.Column<string>(type: "jsonb", nullable: true),
                    sarif_sha256 = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scans", x => x.id);
                    table.CheckConstraint("ck_scans_commit_sha_format", "commit_sha ~ '^[0-9a-f]{7,64}$'");
                    table.CheckConstraint("ck_scans_gate_result", "gate_result IN ('Passed', 'Failed')");
                    table.CheckConstraint("ck_scans_number_positive", "number > 0");
                    table.CheckConstraint("ck_scans_one_uploader", "num_nonnulls(uploaded_by_user_id, uploaded_by_key_id) = 1");
                    table.CheckConstraint("ck_scans_status", "status IN ('Completed', 'Rejected')");
                    table.ForeignKey(
                        name: "fk_scans_api_keys_uploaded_by_key_id",
                        column: x => x.uploaded_by_key_id,
                        principalTable: "api_keys",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_scans_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_scans_users_uploaded_by_user_id",
                        column: x => x.uploaded_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "findings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fingerprint = table.Column<string>(type: "text", nullable: false),
                    severity = table.Column<string>(type: "text", nullable: false),
                    lifecycle = table.Column<string>(type: "text", nullable: false),
                    triage_status = table.Column<string>(type: "text", nullable: false, defaultValue: "Untriaged"),
                    accepted_risk_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    file_path = table.Column<string>(type: "text", nullable: false),
                    start_line = table.Column<int>(type: "integer", nullable: true),
                    message = table.Column<string>(type: "text", nullable: false),
                    first_seen_scan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_seen_scan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    severity_rank = table.Column<short>(type: "smallint", nullable: false, computedColumnSql: "CASE severity WHEN 'Critical' THEN 4 WHEN 'High' THEN 3 WHEN 'Medium' THEN 2 ELSE 1 END", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_findings", x => x.id);
                    table.CheckConstraint("ck_findings_accepted_risk_expiry", "(triage_status = 'AcceptedRisk') = (accepted_risk_expires_at IS NOT NULL)");
                    table.CheckConstraint("ck_findings_lifecycle", "lifecycle IN ('New', 'Existing', 'Reopened', 'Resolved')");
                    table.CheckConstraint("ck_findings_severity", "severity IN ('Low', 'Medium', 'High', 'Critical')");
                    table.CheckConstraint("ck_findings_start_line_positive", "start_line > 0");
                    table.CheckConstraint("ck_findings_triage_status", "triage_status IN ('Untriaged', 'Confirmed', 'FalsePositive', 'AcceptedRisk')");
                    table.ForeignKey(
                        name: "fk_findings_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_findings_rules_rule_id",
                        column: x => x.rule_id,
                        principalTable: "rules",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_findings_scans_first_seen_scan_id",
                        column: x => x.first_seen_scan_id,
                        principalTable: "scans",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_findings_scans_last_seen_scan_id",
                        column: x => x.last_seen_scan_id,
                        principalTable: "scans",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "scan_artifacts",
                columns: table => new
                {
                    scan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_bytes = table.Column<long>(type: "bigint", nullable: false),
                    content_gzip = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scan_artifacts", x => x.scan_id);
                    table.CheckConstraint("ck_scan_artifacts_original_bytes", "original_bytes > 0");
                    table.ForeignKey(
                        name: "fk_scan_artifacts_scans_scan_id",
                        column: x => x.scan_id,
                        principalTable: "scans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scan_tools",
                columns: table => new
                {
                    scan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tool_name = table.Column<string>(type: "text", nullable: false),
                    tool_version = table.Column<string>(type: "text", nullable: true),
                    result_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scan_tools", x => new { x.scan_id, x.tool_name });
                    table.ForeignKey(
                        name: "fk_scan_tools_scans_scan_id",
                        column: x => x.scan_id,
                        principalTable: "scans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "finding_occurrences",
                columns: table => new
                {
                    scan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    finding_id = table.Column<Guid>(type: "uuid", nullable: false),
                    change = table.Column<string>(type: "text", nullable: false),
                    severity = table.Column<string>(type: "text", nullable: false),
                    file_path = table.Column<string>(type: "text", nullable: false),
                    start_line = table.Column<int>(type: "integer", nullable: true),
                    start_column = table.Column<int>(type: "integer", nullable: true),
                    end_line = table.Column<int>(type: "integer", nullable: true),
                    message = table.Column<string>(type: "text", nullable: false),
                    snippet = table.Column<string>(type: "text", nullable: true),
                    partial_fingerprints = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_finding_occurrences", x => new { x.scan_id, x.finding_id });
                    table.CheckConstraint("ck_finding_occurrences_change", "change IN ('New', 'Existing', 'Reopened')");
                    table.CheckConstraint("ck_finding_occurrences_severity", "severity IN ('Low', 'Medium', 'High', 'Critical')");
                    table.CheckConstraint("ck_finding_occurrences_start_column_positive", "start_column > 0");
                    table.CheckConstraint("ck_finding_occurrences_start_line_positive", "start_line > 0");
                    table.ForeignKey(
                        name: "fk_finding_occurrences_findings_finding_id",
                        column: x => x.finding_id,
                        principalTable: "findings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_finding_occurrences_scans_scan_id",
                        column: x => x.scan_id,
                        principalTable: "scans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "finding_resolutions",
                columns: table => new
                {
                    scan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    finding_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_finding_resolutions", x => new { x.scan_id, x.finding_id });
                    table.ForeignKey(
                        name: "fk_finding_resolutions_findings_finding_id",
                        column: x => x.finding_id,
                        principalTable: "findings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_finding_resolutions_scans_scan_id",
                        column: x => x.scan_id,
                        principalTable: "scans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "triage_decisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    finding_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    decided_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_triage_decisions", x => x.id);
                    table.CheckConstraint("ck_triage_decisions_accepted_risk_expiry", "(status = 'AcceptedRisk') = (expires_at IS NOT NULL)");
                    table.CheckConstraint("ck_triage_decisions_reason_length", "length(reason) <= 1000");
                    table.CheckConstraint("ck_triage_decisions_reason_required", "status = 'Confirmed' OR decided_by_id IS NULL OR length(reason) >= 10");
                    table.CheckConstraint("ck_triage_decisions_status", "status IN ('Untriaged', 'Confirmed', 'FalsePositive', 'AcceptedRisk')");
                    table.ForeignKey(
                        name: "fk_triage_decisions_findings_finding_id",
                        column: x => x.finding_id,
                        principalTable: "findings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_triage_decisions_users_decided_by_id",
                        column: x => x.decided_by_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_api_keys_created_by_id",
                table: "api_keys",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_api_keys_prefix",
                table: "api_keys",
                column: "prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_api_keys_project_id_name",
                table: "api_keys",
                columns: new[] { "project_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_audit_actor_time",
                table: "audit_events",
                columns: new[] { "actor_id", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_audit_project_time",
                table: "audit_events",
                columns: new[] { "project_id", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_occurrences_finding",
                table: "finding_occurrences",
                columns: new[] { "finding_id", "scan_id" });

            migrationBuilder.CreateIndex(
                name: "ix_resolutions_finding",
                table: "finding_resolutions",
                column: "finding_id");

            migrationBuilder.CreateIndex(
                name: "ix_findings_file_trgm",
                table: "findings",
                column: "file_path")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_findings_first_seen_scan_id",
                table: "findings",
                column: "first_seen_scan_id");

            migrationBuilder.CreateIndex(
                name: "ix_findings_last_seen_scan_id",
                table: "findings",
                column: "last_seen_scan_id");

            migrationBuilder.CreateIndex(
                name: "ix_findings_message_trgm",
                table: "findings",
                column: "message")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_findings_open_severity",
                table: "findings",
                columns: new[] { "project_id", "severity_rank", "id" },
                descending: new[] { false, true, false },
                filter: "lifecycle <> 'Resolved'");

            migrationBuilder.CreateIndex(
                name: "ix_findings_project_id_fingerprint",
                table: "findings",
                columns: new[] { "project_id", "fingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_findings_project_last_seen",
                table: "findings",
                columns: new[] { "project_id", "last_seen_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_findings_risk_expiry",
                table: "findings",
                column: "accepted_risk_expires_at",
                filter: "triage_status = 'AcceptedRisk'");

            migrationBuilder.CreateIndex(
                name: "ix_findings_rule",
                table: "findings",
                column: "rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_members_user",
                table: "project_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_projects_created_by_id",
                table: "projects",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_projects_key",
                table: "projects",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rules_tool_name_rule_id",
                table: "rules",
                columns: new[] { "tool_name", "rule_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_scans_project_id_number",
                table: "scans",
                columns: new[] { "project_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_scans_project_sha",
                table: "scans",
                columns: new[] { "project_id", "sarif_sha256" });

            migrationBuilder.CreateIndex(
                name: "ix_scans_project_uploaded",
                table: "scans",
                columns: new[] { "project_id", "uploaded_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_scans_uploaded_by_key_id",
                table: "scans",
                column: "uploaded_by_key_id");

            migrationBuilder.CreateIndex(
                name: "ix_scans_uploaded_by_user_id",
                table: "scans",
                column: "uploaded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_triage_decisions_decided_by_id",
                table: "triage_decisions",
                column: "decided_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_triage_finding",
                table: "triage_decisions",
                columns: new[] { "finding_id", "decided_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_users_normalized_email",
                table: "users",
                column: "normalized_email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_events");

            migrationBuilder.DropTable(
                name: "finding_occurrences");

            migrationBuilder.DropTable(
                name: "finding_resolutions");

            migrationBuilder.DropTable(
                name: "project_members");

            migrationBuilder.DropTable(
                name: "scan_artifacts");

            migrationBuilder.DropTable(
                name: "scan_tools");

            migrationBuilder.DropTable(
                name: "triage_decisions");

            migrationBuilder.DropTable(
                name: "findings");

            migrationBuilder.DropTable(
                name: "rules");

            migrationBuilder.DropTable(
                name: "scans");

            migrationBuilder.DropTable(
                name: "api_keys");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
