ALTER TABLE companies ADD COLUMN custom_domain varchar(255);
CREATE UNIQUE INDEX ix_companies_custom_domain ON companies (custom_domain) WHERE custom_domain IS NOT NULL;
