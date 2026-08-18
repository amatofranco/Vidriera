-- Historial de pedidos: cada vez que se genera el Excel de un pedido desde
-- el catálogo público, se guarda una copia (datos del cliente + snapshot de
-- productos/ISBN/cantidad) para que el dueño de la empresa la vea en el
-- panel admin.

CREATE TABLE orders (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id          uuid NOT NULL REFERENCES companies (id),
    business_name       varchar(300) NOT NULL,
    store_name          varchar(300) NULL,
    cuit                varchar(50) NOT NULL,
    vat_condition       varchar(100) NULL,
    phone               varchar(100) NULL,
    email               varchar(300) NOT NULL,
    city                varchar(200) NULL,
    province            varchar(200) NULL,
    carrier             varchar(200) NULL,
    delivery_address    varchar(500) NULL,
    items_snapshot_json text NOT NULL DEFAULT '[]',
    created_at          timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX ix_orders_company_id ON orders (company_id);
