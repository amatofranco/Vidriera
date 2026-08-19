-- El "código" original (previo al ISBN) nunca se usó en la UI; se descarta
-- y se reusa el nombre "code" para el campo que hasta ahora era ISBN, ahora
-- unificado como "Código" en toda la app.

ALTER TABLE products DROP COLUMN code;
ALTER TABLE products RENAME COLUMN isbn TO code;
