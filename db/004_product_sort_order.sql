ALTER TABLE products ADD COLUMN sort_order integer NOT NULL DEFAULT 0;

-- Backfill: orden alfabético actual como punto de partida, por empresa.
UPDATE products p
SET sort_order = sub.rn
FROM (
    SELECT id, row_number() OVER (PARTITION BY company_id ORDER BY name) - 1 AS rn
    FROM products
) sub
WHERE p.id = sub.id;

CREATE INDEX ix_products_company_sort_order ON products (company_id, sort_order);
