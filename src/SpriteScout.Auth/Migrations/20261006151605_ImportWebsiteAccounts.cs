using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpriteScout.Auth.Migrations
{
    /// <inheritdoc />
    public partial class ImportWebsiteAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 1's one-time cross-schema import runs after the host's website migrations.
            // EF records this migration in the auth history and wraps it in a transaction.
            migrationBuilder.Sql("""
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
                SELECT u."Id" AS user_id, u."GoogleSubject" AS subject,
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
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Imported identities may already own grants and tokens. Rolling migration history
            // back must not delete those accounts or disconnect existing website profiles.
        }
    }
}
