CREATE TABLE catalog_generation_jobs (
    id uuid PRIMARY KEY,
    company_id uuid NOT NULL REFERENCES companies(id),
    user_id uuid NOT NULL REFERENCES users(id),
    show_prices boolean NOT NULL DEFAULT false,
    status text NOT NULL DEFAULT 'Pending',
    stage text,
    progress_current integer NOT NULL DEFAULT 0,
    progress_total integer NOT NULL DEFAULT 0,
    result_catalog_id uuid,
    result_url text,
    error_message text,
    created_at timestamp NOT NULL DEFAULT now(),
    updated_at timestamp NOT NULL DEFAULT now()
);

CREATE INDEX idx_catalog_generation_jobs_status ON catalog_generation_jobs (status, created_at);
