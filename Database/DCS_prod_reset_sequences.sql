-- Ejecutar en PROD despues de cargar el catalogo con IDs explicitos.
-- Las secuencias empiezan en 1; sin esto, el proximo registro que se agregue sin ID
-- choca con los IDs copiados de test (ORA-00001).
DECLARE
    PROCEDURE reset_seq(p_seq VARCHAR2, p_table VARCHAR2, p_col VARCHAR2) IS
        v_next NUMBER;
    BEGIN
        EXECUTE IMMEDIATE 'SELECT NVL(MAX(' || p_col || '), 0) + 1 FROM UNIS_INTERFACES.' || p_table INTO v_next;
        EXECUTE IMMEDIATE 'DROP SEQUENCE UNIS_INTERFACES.' || p_seq;
        EXECUTE IMMEDIATE 'CREATE SEQUENCE UNIS_INTERFACES.' || p_seq || ' START WITH ' || v_next || ' INCREMENT BY 1 NOCACHE';
    END;
BEGIN
    reset_seq('SEQ_DCS_DEPARTAMENTO',   'TBL_DCS_DEPARTAMENTO',   'ID_DEPARTAMENTO');
    reset_seq('SEQ_DCS_CATEGORIA',      'TBL_DCS_CATEGORIA',      'ID_CATEGORIA');
    reset_seq('SEQ_DCS_TIPO_SOLICITUD', 'TBL_DCS_TIPO_SOLICITUD', 'ID_TIPO_SOLICITUD');
END;
/

-- Solicitudes: arrancar por encima de cualquier ID usado en test. Las pruebas contra Fusion prod
-- con la BD de test ya crearon SRs con [DCS-<id de test>] en el titulo (ej. SR-1106176 = [DCS-35]);
-- si prod reutiliza ese ID, la busqueda puede vincular la solicitud nueva al SR viejo.
DROP SEQUENCE UNIS_INTERFACES.SEQ_DCS_SOLICITUD;
CREATE SEQUENCE UNIS_INTERFACES.SEQ_DCS_SOLICITUD START WITH 1000 INCREMENT BY 1 NOCACHE;
-- Los triggers quedan INVALID por el DROP y Oracle los recompila solo en el siguiente INSERT.
