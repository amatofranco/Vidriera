CREATE TABLE company_subscriptions (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id        uuid NOT NULL UNIQUE REFERENCES companies (id),
    plan              varchar(20) NOT NULL,
    plan_amount_usd   numeric(10,2) NOT NULL,
    usd_ars_rate      numeric(10,2) NOT NULL,
    amount_ars        numeric(10,2) NOT NULL,
    preapproval_id    varchar(100) NOT NULL,
    status            varchar(50) NOT NULL,
    access_expires_at timestamptz NULL,
    created_at        timestamptz NOT NULL DEFAULT now(),
    updated_at        timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX ix_company_subscriptions_preapproval_id ON company_subscriptions (preapproval_id);

CREATE TABLE processed_mercadopago_payments (
    id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_id   varchar(100) NOT NULL UNIQUE,
    processed_at timestamptz NOT NULL DEFAULT now()
);
