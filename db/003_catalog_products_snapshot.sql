-- Products can now be hard-deleted (row + R2 blob). Historical catalogs no
-- longer reference products by FK -- they keep a frozen JSON snapshot
-- (id/name/code) of what was included at generation time instead, so
-- deleting a product later doesn't break past catalogs or get blocked by a
-- foreign key.

ALTER TABLE generated_catalogs ADD COLUMN products_snapshot text NOT NULL DEFAULT '[]';

DROP TABLE generated_catalog_products;
