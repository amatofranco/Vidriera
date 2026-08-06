ALTER TABLE sections ADD COLUMN parent_section_id uuid REFERENCES sections (id);
CREATE INDEX ix_sections_parent_section_id ON sections (parent_section_id);
