ALTER TABLE companies ADD COLUMN current_catalog_id uuid;
ALTER TABLE generated_catalogs DROP COLUMN expires_at;
ALTER TABLE generated_catalogs DROP COLUMN status;
