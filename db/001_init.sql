-- Vidriera — esquema inicial (PostgreSQL / Neon)
-- Corresponde a los mappings de NHibernate en Vidriera.Infrastructure.Persistence.Mappings

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE companies (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name        varchar(200) NOT NULL,
    is_active   boolean NOT NULL DEFAULT true,
    created_at  timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE users (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id      uuid NOT NULL REFERENCES companies (id),
    email           varchar(320) NOT NULL UNIQUE,
    name            varchar(200) NOT NULL,
    password_hash   varchar(500) NOT NULL,
    is_active       boolean NOT NULL DEFAULT true
);

CREATE INDEX ix_users_company_id ON users (company_id);

CREATE TABLE products (
    id                          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id                  uuid NOT NULL REFERENCES companies (id),
    name                        varchar(300) NOT NULL,
    code                        varchar(100) NOT NULL,
    sheet_pdf_blob_key          varchar(500) NULL,
    sheet_pdf_original_name     varchar(300) NULL,
    has_stock                   boolean NOT NULL DEFAULT false,
    is_active                   boolean NOT NULL DEFAULT true
);

CREATE INDEX ix_products_company_id ON products (company_id);

-- Estado del catálogo generado: 0 = Active, 1 = Expired, 2 = Revoked
-- (mismo orden que el enum Vidriera.Domain.Enums.CatalogStatus)
CREATE TABLE generated_catalogs (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id              uuid NOT NULL REFERENCES companies (id),
    user_id                 uuid NOT NULL REFERENCES users (id),
    generated_at            timestamptz NOT NULL DEFAULT now(),
    generated_pdf_blob_key  varchar(500) NOT NULL,
    expires_at              timestamptz NULL,
    status                  smallint NOT NULL DEFAULT 0
);

CREATE INDEX ix_generated_catalogs_company_id ON generated_catalogs (company_id);

CREATE TABLE generated_catalog_products (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    generated_catalog_id    uuid NOT NULL REFERENCES generated_catalogs (id) ON DELETE CASCADE,
    product_id              uuid NOT NULL REFERENCES products (id)
);

CREATE INDEX ix_generated_catalog_products_catalog_id ON generated_catalog_products (generated_catalog_id);
CREATE INDEX ix_generated_catalog_products_product_id ON generated_catalog_products (product_id);
