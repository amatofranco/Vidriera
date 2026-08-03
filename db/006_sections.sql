CREATE TABLE sections (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id              uuid NOT NULL REFERENCES companies (id),
    name                    varchar(300) NOT NULL,
    cover_pdf_blob_key      varchar(500) NOT NULL,
    cover_pdf_original_name varchar(300) NOT NULL,
    sort_order              integer NOT NULL DEFAULT 0
);

CREATE INDEX ix_sections_company_id ON sections (company_id);

ALTER TABLE products ADD COLUMN section_id uuid REFERENCES sections (id);

CREATE INDEX ix_products_section_id ON products (section_id);
