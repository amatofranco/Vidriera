CREATE TABLE password_reset_tokens (
    id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id    uuid NOT NULL REFERENCES users (id),
    token_hash varchar(100) NOT NULL UNIQUE,
    expires_at timestamptz NOT NULL,
    used_at    timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX ix_password_reset_tokens_user_id ON password_reset_tokens (user_id);
