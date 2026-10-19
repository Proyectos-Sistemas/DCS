-- Ejecutar como UNIS_INTERFACES, ANTES de desplegar el codigo que mapea NUMERO_SR
-- (EF falla en cualquier consulta a TBL_DCS_SOLICITUD si la columna no existe).
-- Guarda el SR de Oracle Fusion vinculado a la solicitud; null mientras no se encuentra.

ALTER TABLE UNIS_INTERFACES.TBL_DCS_SOLICITUD ADD (NUMERO_SR VARCHAR2(30));
