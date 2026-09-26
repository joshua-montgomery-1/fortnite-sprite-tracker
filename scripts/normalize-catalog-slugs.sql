-- Run after the RenameVariantStyleImageSuffixToSlug migration has been
-- deployed. This script changes catalog identifiers only; image paths are
-- intentionally untouched.
--
-- The guarded predicates make this safe to run more than once. It only
-- changes the expected legacy value for each known catalog row.

begin;

update "SpriteFamilies" set "Slug" = 'aura' where "Id" = 10 and "Slug" = 'drifter';
update "SpriteFamilies" set "Slug" = 'striker' where "Id" = 11 and "Slug" = 'soccer';
update "SpriteFamilies" set "Slug" = 'grim-reaper' where "Id" = 18 and "Slug" = 'grimreaper';
update "SpriteFamilies" set "Slug" = 'zero-point' where "Id" = 19 and "Slug" = 'zeropoint';
update "SpriteFamilies" set "Slug" = 'burnt-peanut' where "Id" = 20 and "Slug" = 'theburntpeanut';
update "SpriteFamilies" set "Slug" = 'vini-jr' where "Id" = 23 and "Slug" = 'vinijr';
update "SpriteFamilies" set "Slug" = 'john-wick' where "Id" = 24 and "Slug" = 'johnwick';
update "SpriteFamilies" set "Slug" = '8-bit' where "Id" = 27 and "Slug" = '8bit';
update "SpriteFamilies" set "Slug" = 'storm-scout' where "Id" = 37 and "Slug" = 'stormscout';
update "SpriteFamilies" set "Slug" = 'mega-man' where "Id" = 39 and "Slug" = 'megaman';
update "SpriteFamilies" set "Slug" = 'x-ray' where "Id" = 40 and "Slug" = 'xray';
update "SpriteFamilies" set "Slug" = 'crash-bandicoot' where "Id" = 42 and "Slug" = 'crashbandicoot';

update "VariantStyles" set "Slug" = 'normal' where "Id" = 1 and "Slug" = 'basic';
update "VariantStyles" set "Slug" = 'cheat-master' where "Id" = 9 and "Slug" = 'cheatmaster';
update "VariantStyles" set "Slug" = 'loot-hacker' where "Id" = 10 and "Slug" = 'hacker';
update "VariantStyles" set "Slug" = 'bounty-hunter' where "Id" = 13 and "Slug" = 'bountyhunter';

do $$
begin
    if exists (
        select 1 from "SpriteFamilies"
        where ("Id", "Slug") in (
            (10, 'drifter'), (11, 'soccer'), (18, 'grimreaper'),
            (19, 'zeropoint'), (20, 'theburntpeanut'), (23, 'vinijr'),
            (24, 'johnwick'), (27, '8bit'), (37, 'stormscout'),
            (39, 'megaman'), (40, 'xray'), (42, 'crashbandicoot')
        )
    ) then
        raise exception 'One or more legacy family slugs remain unchanged.';
    end if;

    if exists (
        select 1 from "VariantStyles"
        where ("Id", "Slug") in (
            (1, 'basic'), (9, 'cheatmaster'), (10, 'hacker'), (13, 'bountyhunter')
        )
    ) then
        raise exception 'One or more legacy variant slugs remain unchanged.';
    end if;
end $$;

commit;
