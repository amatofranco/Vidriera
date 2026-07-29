-- El código de producto ya no se pide al crear (solo el nombre, tomado del
-- PDF subido); pasa a ser opcional.

ALTER TABLE products ALTER COLUMN code DROP NOT NULL;
