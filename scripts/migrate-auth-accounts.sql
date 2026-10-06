-- Sprite Scout Phase 1: one-time production schema and account migration.
-- Run BEFORE deploying this Phase 1 release, against the existing website database.
-- Requires the current main website migrations through
-- 20260926161558_RenameVariantStyleImageSuffixToSlug and permission to create
-- sprite_scout_auth tables and alter public."Users". PostgreSQL 13+.
-- This is a fresh rollout script, not an upgrade for previous development schemas.
-- Creates auth/OpenIddict tables, adds Users.AccountId, imports Google identities,
-- and records the two EF migrations. Existing user IDs/profiles/progress are retained.
-- Does not register OAuth clients, enable production OAuth, or change credentials.
-- Execute the complete file in one database session. Any failure rolls back everything.

BEGIN;
SET LOCAL search_path = public;
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'sprite_scout_auth') THEN
        CREATE SCHEMA sprite_scout_auth;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS sprite_scout_auth."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'sprite_scout_auth') THEN
        CREATE SCHEMA sprite_scout_auth;
    END IF;
END $EF$;

CREATE TABLE sprite_scout_auth."Accounts" (
    "Id" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Accounts" PRIMARY KEY ("Id")
);

CREATE TABLE sprite_scout_auth."OpenIddictApplications" (
    "Id" text NOT NULL,
    "ApplicationType" character varying(50),
    "ClientId" character varying(100),
    "ClientSecret" text,
    "ClientType" character varying(50),
    "ConcurrencyToken" character varying(50),
    "ConsentType" character varying(50),
    "DisplayName" text,
    "DisplayNames" text,
    "JsonWebKeySet" text,
    "Permissions" text,
    "PostLogoutRedirectUris" text,
    "Properties" text,
    "RedirectUris" text,
    "Requirements" text,
    "Settings" text,
    CONSTRAINT "PK_OpenIddictApplications" PRIMARY KEY ("Id")
);

CREATE TABLE sprite_scout_auth."OpenIddictScopes" (
    "Id" text NOT NULL,
    "ConcurrencyToken" character varying(50),
    "Description" text,
    "Descriptions" text,
    "DisplayName" text,
    "DisplayNames" text,
    "Name" character varying(200),
    "Properties" text,
    "Resources" text,
    CONSTRAINT "PK_OpenIddictScopes" PRIMARY KEY ("Id")
);

CREATE TABLE sprite_scout_auth."ExternalIdentities" (
    "Id" uuid NOT NULL,
    "AccountId" uuid NOT NULL,
    "Provider" character varying(80) NOT NULL,
    "Issuer" character varying(255) NOT NULL,
    "Subject" character varying(255) NOT NULL,
    CONSTRAINT "PK_ExternalIdentities" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ExternalIdentities_Accounts_AccountId" FOREIGN KEY ("AccountId") REFERENCES sprite_scout_auth."Accounts" ("Id") ON DELETE CASCADE
);

CREATE TABLE sprite_scout_auth."OpenIddictAuthorizations" (
    "Id" text NOT NULL,
    "ApplicationId" text,
    "ConcurrencyToken" character varying(50),
    "CreationDate" timestamp with time zone,
    "Properties" text,
    "Scopes" text,
    "Status" character varying(50),
    "Subject" character varying(400),
    "Type" character varying(50),
    CONSTRAINT "PK_OpenIddictAuthorizations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_OpenIddictAuthorizations_OpenIddictApplications_Application~" FOREIGN KEY ("ApplicationId") REFERENCES sprite_scout_auth."OpenIddictApplications" ("Id")
);

CREATE TABLE sprite_scout_auth."OpenIddictTokens" (
    "Id" text NOT NULL,
    "ApplicationId" text,
    "AuthorizationId" text,
    "ConcurrencyToken" character varying(50),
    "CreationDate" timestamp with time zone,
    "ExpirationDate" timestamp with time zone,
    "Payload" text,
    "Properties" text,
    "RedemptionDate" timestamp with time zone,
    "ReferenceId" character varying(100),
    "Status" character varying(50),
    "Subject" character varying(400),
    "Type" character varying(150),
    CONSTRAINT "PK_OpenIddictTokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_OpenIddictTokens_OpenIddictApplications_ApplicationId" FOREIGN KEY ("ApplicationId") REFERENCES sprite_scout_auth."OpenIddictApplications" ("Id"),
    CONSTRAINT "FK_OpenIddictTokens_OpenIddictAuthorizations_AuthorizationId" FOREIGN KEY ("AuthorizationId") REFERENCES sprite_scout_auth."OpenIddictAuthorizations" ("Id")
);

CREATE INDEX "IX_ExternalIdentities_AccountId" ON sprite_scout_auth."ExternalIdentities" ("AccountId");

CREATE UNIQUE INDEX "IX_ExternalIdentities_Provider_Issuer_Subject" ON sprite_scout_auth."ExternalIdentities" ("Provider", "Issuer", "Subject");

CREATE UNIQUE INDEX "IX_OpenIddictApplications_ClientId" ON sprite_scout_auth."OpenIddictApplications" ("ClientId");

CREATE INDEX "IX_OpenIddictAuthorizations_ApplicationId_Status_Subject_Type" ON sprite_scout_auth."OpenIddictAuthorizations" ("ApplicationId", "Status", "Subject", "Type");

CREATE UNIQUE INDEX "IX_OpenIddictScopes_Name" ON sprite_scout_auth."OpenIddictScopes" ("Name");

CREATE INDEX "IX_OpenIddictTokens_ApplicationId_Status_Subject_Type" ON sprite_scout_auth."OpenIddictTokens" ("ApplicationId", "Status", "Subject", "Type");

CREATE INDEX "IX_OpenIddictTokens_AuthorizationId" ON sprite_scout_auth."OpenIddictTokens" ("AuthorizationId");

CREATE UNIQUE INDEX "IX_OpenIddictTokens_ReferenceId" ON sprite_scout_auth."OpenIddictTokens" ("ReferenceId");

INSERT INTO sprite_scout_auth."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261006034539_CreateAuth', '10.0.11');


ALTER TABLE "Users" ADD "AccountId" uuid;

CREATE UNIQUE INDEX "IX_Users_AccountId" ON "Users" ("AccountId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261006034616_AddAccountReference', '10.0.11');


-- BEGIN ACCOUNT IMPORT
LOCK TABLE public."Users", sprite_scout_auth."Accounts", sprite_scout_auth."ExternalIdentities"
IN SHARE ROW EXCLUSIVE MODE;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM public."Users" u
        JOIN sprite_scout_auth."ExternalIdentities" i
          ON i."Provider" = 'Google' AND i."Issuer" = 'https://accounts.google.com'
         AND i."Subject" = u."GoogleSubject"
        WHERE u."AccountId" IS NOT NULL AND u."AccountId" <> i."AccountId"
    ) THEN
        RAISE EXCEPTION 'A website account has a conflicting Google account mapping';
    END IF;
END $$;

CREATE TEMP TABLE sprite_scout_account_import ON COMMIT DROP AS
SELECT u."GoogleSubject" AS subject,
       COALESCE(i."AccountId", u."AccountId", gen_random_uuid()) AS account_id,
       u."CreatedAtUtc" AS created_at
FROM public."Users" u
LEFT JOIN sprite_scout_auth."ExternalIdentities" i
  ON i."Provider" = 'Google' AND i."Issuer" = 'https://accounts.google.com'
 AND i."Subject" = u."GoogleSubject";

INSERT INTO sprite_scout_auth."Accounts" ("Id", "CreatedAtUtc")
SELECT account_id, min(created_at) FROM sprite_scout_account_import GROUP BY account_id
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO sprite_scout_auth."ExternalIdentities" ("Id", "AccountId", "Provider", "Issuer", "Subject")
SELECT gen_random_uuid(), account_id, 'Google', 'https://accounts.google.com', subject
FROM sprite_scout_account_import
ON CONFLICT ("Provider", "Issuer", "Subject") DO NOTHING;

UPDATE public."Users" u SET "AccountId" = i."AccountId"
FROM sprite_scout_auth."ExternalIdentities" i
WHERE i."Provider" = 'Google' AND i."Issuer" = 'https://accounts.google.com'
  AND i."Subject" = u."GoogleSubject" AND u."AccountId" IS NULL;

DROP TABLE sprite_scout_account_import;

-- END ACCOUNT IMPORT

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM public."Users" u
        LEFT JOIN sprite_scout_auth."Accounts" a ON a."Id" = u."AccountId"
        LEFT JOIN sprite_scout_auth."ExternalIdentities" i
          ON i."AccountId" = u."AccountId" AND i."Provider" = 'Google'
         AND i."Issuer" = 'https://accounts.google.com' AND i."Subject" = u."GoogleSubject"
        WHERE a."Id" IS NULL OR i."Id" IS NULL
    ) THEN
        RAISE EXCEPTION 'Account import verification failed; transaction will roll back';
    END IF;
END $$;

COMMIT;

SELECT count(*) AS website_users, count("AccountId") AS linked_users FROM public."Users";
