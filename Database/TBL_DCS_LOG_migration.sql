-- DCS DB logging: log4net AdoNetAppender target table.
-- Run connected as UNIS_INTERFACES (same schema as TBL_DCS_SOLICITUD etc.), then grant to the
-- app login UNIS_REPORTES below. The app's Web.config connects as UNIS_REPORTES, which has no
-- default schema containing this table, so Web.config's AdoNetAppender commandText must reference
-- UNIS_INTERFACES.TBL_DCS_LOG explicitly (unqualified names resolve against UNIS_REPORTES's own
-- schema and raise ORA-00942).

CREATE SEQUENCE SEQ_DCS_LOG START WITH 1 INCREMENT BY 1 NOCACHE;

CREATE TABLE TBL_DCS_LOG (
    ID_LOG      NUMBER        NOT NULL,
    FECHA_LOG   TIMESTAMP     NOT NULL,
    NIVEL       VARCHAR2(20)  NOT NULL,
    LOGGER      VARCHAR2(200),
    MENSAJE     VARCHAR2(4000),
    EXCEPCION   VARCHAR2(4000),
    CONSTRAINT PK_DCS_LOG PRIMARY KEY (ID_LOG)
);

CREATE OR REPLACE TRIGGER TRG_DCS_LOG_BI
BEFORE INSERT ON TBL_DCS_LOG
FOR EACH ROW
BEGIN
    IF :NEW.ID_LOG IS NULL THEN
        SELECT SEQ_DCS_LOG.NEXTVAL INTO :NEW.ID_LOG FROM DUAL;
    END IF;
END;
/

GRANT SELECT, INSERT ON TBL_DCS_LOG TO UNIS_REPORTES;
GRANT SELECT ON SEQ_DCS_LOG TO UNIS_REPORTES;
