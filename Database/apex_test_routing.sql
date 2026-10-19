-- TEST: ruteo por tipo en un solo bloque (APEX SQL Commands ejecuta un comando a la vez).
-- Pegar completo y ejecutar como UNIS_INTERFACES. Se puede re-ejecutar: ignora lo que ya existe.
DECLARE
    PROCEDURE ddl(p_sql VARCHAR2) IS
    BEGIN
        EXECUTE IMMEDIATE p_sql;
    EXCEPTION
        -- -1430 columna ya existe, -2275 FK ya existe, -1451 ya es nullable
        WHEN OTHERS THEN
            IF SQLCODE NOT IN (-1430, -2275, -1451) THEN RAISE; END IF;
    END;

    PROCEDURE cola(p_id NUMBER, p_nombre VARCHAR2, p_correo VARCHAR2) IS
    BEGIN
        INSERT INTO UNIS_INTERFACES.TBL_DCS_DEPARTAMENTO (ID_DEPARTAMENTO, NOMBRE_DEPTO, CORREO_NOTIFICACION, CORREO_EXTERNO)
        VALUES (p_id, p_nombre, p_correo, p_correo);
    EXCEPTION
        WHEN DUP_VAL_ON_INDEX THEN
            UPDATE UNIS_INTERFACES.TBL_DCS_DEPARTAMENTO
               SET NOMBRE_DEPTO = p_nombre, CORREO_NOTIFICACION = p_correo, CORREO_EXTERNO = p_correo
             WHERE ID_DEPARTAMENTO = p_id;
    END;

    -- Dinamico: la columna ID_DEPARTAMENTO no existe cuando se compila el bloque.
    PROCEDURE rutear(p_depto NUMBER, p_tipos VARCHAR2) IS
    BEGIN
        EXECUTE IMMEDIATE 'UPDATE UNIS_INTERFACES.TBL_DCS_TIPO_SOLICITUD SET ID_DEPARTAMENTO = ' || p_depto
                       || ' WHERE ID_TIPO_SOLICITUD IN (' || p_tipos || ')';
    END;
BEGIN
    ddl('ALTER TABLE UNIS_INTERFACES.TBL_DCS_TIPO_SOLICITUD ADD (ID_DEPARTAMENTO NUMBER(10))');
    ddl('ALTER TABLE UNIS_INTERFACES.TBL_DCS_TIPO_SOLICITUD ADD CONSTRAINT FK_DCS_TIPO_DEPTO FOREIGN KEY (ID_DEPARTAMENTO) REFERENCES UNIS_INTERFACES.TBL_DCS_DEPARTAMENTO (ID_DEPARTAMENTO)');
    ddl('ALTER TABLE UNIS_INTERFACES.TBL_DCS_CATEGORIA MODIFY (ID_DEPARTAMENTO NULL)');
    -- Colas sin canal en Fusion van sin correo (la app las deja en ERROR); test las tenia NOT NULL.
    ddl('ALTER TABLE UNIS_INTERFACES.TBL_DCS_DEPARTAMENTO MODIFY (CORREO_NOTIFICACION NULL)');
    ddl('ALTER TABLE UNIS_INTERFACES.TBL_DCS_DEPARTAMENTO MODIFY (CORREO_EXTERNO NULL)');

    -- Colas (de TBL_PRODUCTO_DCS.COLA, con la direccion que existe hoy en Fusion)
    cola(101, 'Cola Telecomunicaciones',      'serviciotelecom@unis.edu.gt');
    cola(102, 'Cola Soporte',                 'serviciosoporte@unis.edu.gt');    -- antes colasoporte@
    cola(103, 'Cola Registro Académico',      'registroacademico@unis.edu.gt');  -- antes registroacademicos@
    cola(104, 'Cola Desarrollo',              'proyectos_sistemas@unis.edu.gt'); -- antes coladesarrollo@
    cola(105, 'Cola Procesos',                'procesos@unis.edu.gt');
    cola(106, 'Cola Mesa de Ayuda',           'soporte@unis.edu.gt');            -- + tipo 67 (antes telecomunicaciones@)
    cola(107, 'Cola Facturación Tecnología',  NULL);                             -- sin canal en Fusion
    cola(108, 'Cola Compras Tecnología',      'gestioncompratec@unis.edu.gt');   -- antes gestioncomprastec@

    -- Tipo -> cola
    rutear(101, '3,4,5,6,7,8,22,23,37,38,40,43,49,52,54,56,69,74,78,83,88,89,90,91,92,93,94');
    rutear(102, '1,2,9,15,16,17,19,20,33,34,41,42,44,50,55,72,76,79,81,84,87');
    rutear(103, '21,24,25,26,27,29,30,31,39,48,58,70,71,73,85,86');
    rutear(104, '10,11,12,13,45,46,63,64,65,66,77');
    rutear(105, '14,18,28,35,53,80,82');
    rutear(106, '51,59,60,61,62,67,68');
    rutear(107, '32,36,47');
    rutear(108, '57,75');

    COMMIT;
END;
