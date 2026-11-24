-- Read-only PostgreSQL acceptance checks. Run using psql after compose up.
-- A missing table is immediately visible in first query; no records are modified.
SELECT table_name
FROM information_schema.tables
WHERE table_schema = 'public'
  AND table_name IN ('AppUsers','Recipients','Addresses','Offices','Notices',
                     'NoticeStatusHistories','AuditEvents','Notifications',
                     'DeliveryAttempts','Appeals','Documents','RecipientArchives',
                     'Roles','RolePermissions','Broadcasts')
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

-- Every account refers to an existing role (FK) and system roles are present.
SELECT "Name", "IsSystem", (SELECT count(*) FROM "RolePermissions" p WHERE p."RoleName" = r."Name") AS permissions
FROM "Roles" r ORDER BY "IsSystem" DESC, "Name";
SELECT u."Username" FROM "AppUsers" u LEFT JOIN "Roles" r ON r."Name" = u."Role" WHERE r."Name" IS NULL;

-- Finished broadcasts must not keep pending notifications.
SELECT b."Code", count(*) AS pending_notifications
FROM "Broadcasts" b
JOIN "Notices" n ON n."BroadcastId" = b."Id"
JOIN "Notifications" x ON x."NoticeId" = n."Id"
WHERE b."Status" IN ('Completed','CompletedWithErrors') AND x."Status" = 'Pending'
GROUP BY b."Code";

-- Broadcast summary counters match the per-notice notices count.
SELECT b."Code", b."TargetCount", count(n."Id") AS notices
FROM "Broadcasts" b LEFT JOIN "Notices" n ON n."BroadcastId" = b."Id"
GROUP BY b."Id", b."Code", b."TargetCount"
HAVING count(n."Id") <> b."TargetCount";
