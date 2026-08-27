ALTER TABLE companies ADD COLUMN custom_validity_date timestamp;
ALTER TABLE companies ADD COLUMN show_validity_date boolean NOT NULL DEFAULT true;
