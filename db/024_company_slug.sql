ALTER TABLE companies ADD COLUMN slug varchar(100);
CREATE UNIQUE INDEX ix_companies_slug ON companies (slug) WHERE slug IS NOT NULL;
