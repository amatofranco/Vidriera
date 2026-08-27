ALTER TABLE companies ADD COLUMN cover_logo_blob_key varchar(500);
ALTER TABLE companies ADD COLUMN cover_logo_content_type varchar(100);
ALTER TABLE companies ADD COLUMN catalog_subtitle varchar(100);

UPDATE companies SET catalog_subtitle = 'Catálogo' WHERE show_catalog_label = true;

ALTER TABLE companies DROP COLUMN show_catalog_label;
