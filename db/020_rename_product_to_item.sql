ALTER TABLE products RENAME TO items;
ALTER INDEX ix_products_company_id RENAME TO ix_items_company_id;
ALTER INDEX ix_products_company_sort_order RENAME TO ix_items_company_sort_order;
ALTER INDEX ix_products_section_id RENAME TO ix_items_section_id;
ALTER TABLE generated_catalogs RENAME COLUMN products_snapshot TO items_snapshot;
