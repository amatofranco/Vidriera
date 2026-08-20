ALTER TABLE generated_catalogs ADD COLUMN products_snapshot text NOT NULL DEFAULT '[]';

DROP TABLE generated_catalog_products;
