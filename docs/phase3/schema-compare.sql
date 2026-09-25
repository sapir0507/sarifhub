-- Structural fingerprint of a SarifHub database, used to compare the EF Core migration with schema.sql.
-- Lists columns (type, nullability, default, generation), CHECK / foreign key / primary key definitions and index
-- definitions without their names (EF Core and PostgreSQL name constraints differently; the definitions must match).
--
--   psql -d <database created from schema.sql> -At -f schema-compare.sql > reference.txt
--   psql -d <database created by the migration> -At -f schema-compare.sql > migration.txt
--   diff reference.txt migration.txt
-- columns
SELECT 'col', c.table_name::text, c.column_name::text, c.data_type || coalesce('('||c.character_maximum_length||')',''), c.is_nullable,
       coalesce(regexp_replace(c.column_default, '::(text|character varying)', '', 'g'), ''), coalesce(c.generation_expression,'')
FROM information_schema.columns c WHERE c.table_schema='public' AND c.table_name <> '__EFMigrationsHistory'
UNION ALL
-- check constraints (definition only)
SELECT 'check', rel.relname::text, pg_get_constraintdef(con.oid), '', '', '', ''
FROM pg_constraint con JOIN pg_class rel ON rel.oid = con.conrelid JOIN pg_namespace n ON n.oid=rel.relnamespace
WHERE n.nspname='public' AND con.contype='c'
UNION ALL
-- foreign keys
SELECT 'fk', rel.relname::text, pg_get_constraintdef(con.oid), '', '', '', ''
FROM pg_constraint con JOIN pg_class rel ON rel.oid = con.conrelid JOIN pg_namespace n ON n.oid=rel.relnamespace
WHERE n.nspname='public' AND con.contype='f'
UNION ALL
-- primary keys
SELECT 'pk', rel.relname::text, pg_get_constraintdef(con.oid), '', '', '', ''
FROM pg_constraint con JOIN pg_class rel ON rel.oid = con.conrelid JOIN pg_namespace n ON n.oid=rel.relnamespace
WHERE n.nspname='public' AND con.contype='p' AND rel.relname <> '__EFMigrationsHistory'
UNION ALL
-- indexes, name removed (unique constraints appear here as unique indexes)
SELECT 'index', t.relname::text, regexp_replace(pg_get_indexdef(i.indexrelid), 'INDEX \S+ ON', 'INDEX ON'), '', '', '', ''
FROM pg_index i JOIN pg_class t ON t.oid=i.indrelid JOIN pg_namespace n ON n.oid=t.relnamespace
WHERE n.nspname='public' AND NOT i.indisprimary AND t.relname <> '__EFMigrationsHistory'
UNION ALL
SELECT 'extension', extname::text, '', '', '', '', '' FROM pg_extension WHERE extname <> 'plpgsql'
ORDER BY 1,2,3,4;
