CREATE TABLE order_form_fields (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES companies(id),
    label varchar(100) NOT NULL,
    field_type varchar(30) NOT NULL,
    is_required boolean NOT NULL DEFAULT false,
    sort_order integer NOT NULL DEFAULT 0
);
CREATE INDEX ix_order_form_fields_company_id ON order_form_fields (company_id);

ALTER TABLE orders ADD COLUMN customer_fields_json text NOT NULL DEFAULT '[]';
ALTER TABLE orders ALTER COLUMN business_name DROP NOT NULL;
ALTER TABLE orders ALTER COLUMN cuit DROP NOT NULL;
ALTER TABLE orders ALTER COLUMN email DROP NOT NULL;
