-- ISBN opcional por producto, cargable al subir su ficha PDF.

ALTER TABLE products ADD COLUMN isbn varchar(50) NULL;
