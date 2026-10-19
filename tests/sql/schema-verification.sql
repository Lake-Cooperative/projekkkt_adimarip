-- Read-only PostgreSQL acceptance checks. Run using psql after compose up.
-- A missing table is immediately visible in first query; no records are modified.
SELECT table_name
FROM information_schema.tables
WHERE table_schema = 'public'
  AND table_name IN ('AppUsers','Recipients','Addresses','Offices','Notices',
                     'NoticeStatusHistories','AuditEvents','Notifications',
                     'DeliveryAttempts','Appeals','Documents','RecipientArchives')
ORDER BY table_name;

SELECT "CurrentStatus", COUNT(*) AS notice_count
FROM "Notices"
GROUP BY "CurrentStatus"
ORDER BY "CurrentStatus";

-- No orphan notices should be present: both FKs are Restrict.
SELECT COUNT(*) AS orphan_notices FROM "Notices" n
LEFT JOIN "Recipients" r ON r."Id"=n."RecipientId"
LEFT JOIN "Offices" o ON o."Id"=n."OfficeId"
WHERE r."Id" IS NULL OR o."Id" IS NULL;

-- New notices always carry initial history and audit.
SELECT n."Number" FROM "Notices" n
WHERE NOT EXISTS (SELECT 1 FROM "NoticeStatusHistories" h WHERE h."NoticeId"=n."Id")
   OR NOT EXISTS (SELECT 1 FROM "AuditEvents" a WHERE a."NoticeId"=n."Id");

-- Archive rows must not refer to missing recipients.
SELECT COUNT(*) AS orphan_archives FROM "RecipientArchives" a
LEFT JOIN "Recipients" r ON r."Id"=a."RecipientId"
WHERE r."Id" IS NULL;

-- Active accounts have known roles and Observer accounts are tied to recipients.
SELECT "Username", "Role", "RecipientId", length("PasswordHash") > 20 AS has_password_hash
FROM "AppUsers" ORDER BY "Role", "Username";

-- Versioned migrations visible in order, including AppUsers table.
SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";

-- A duplicate notice number or recipient identifier must not be present.
SELECT "Number", count(*) AS duplicates FROM "Notices" GROUP BY "Number" HAVING count(*) > 1;
SELECT "ExternalIdentifier", count(*) AS duplicates FROM "Recipients"
GROUP BY "ExternalIdentifier" HAVING count(*) > 1;
