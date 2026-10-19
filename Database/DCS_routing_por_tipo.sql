-- Ruteo por tipo de solicitud, como en la app vieja (TBL_PRODUCTO_DCS.COLA).
-- Ejecutar como UNIS_INTERFACES en TEST y en PROD (en prod: despues de DCS_prod_create.sql
-- y antes de DCS_prod_catalog.sql). En prod los UPDATE afectan 0 filas: ahi el catalogo ya
-- se inserta con ID_DEPARTAMENTO.
--
-- En la app vieja las categorias no tenian departamento y una misma categoria mezclaba colas
-- (p. ej. "Restablecimiento de contraseña" iba a 4 colas distintas), asi que la relacion
-- categoria -> departamento de la migracion no puede reproducir el ruteo. Ahora cada tipo
-- apunta a su cola; la de la categoria queda solo como respaldo.

ALTER TABLE UNIS_INTERFACES.TBL_DCS_TIPO_SOLICITUD ADD (ID_DEPARTAMENTO NUMBER(10));
ALTER TABLE UNIS_INTERFACES.TBL_DCS_TIPO_SOLICITUD ADD CONSTRAINT FK_DCS_TIPO_DEPTO
    FOREIGN KEY (ID_DEPARTAMENTO) REFERENCES UNIS_INTERFACES.TBL_DCS_DEPARTAMENTO (ID_DEPARTAMENTO);

-- En prod DCS_prod_create.sql la dejo NOT NULL; en prod las categorias se cargan sin departamento.
-- (En test falla con ORA-01451 si ya es nullable: ignorar.)
ALTER TABLE UNIS_INTERFACES.TBL_DCS_CATEGORIA MODIFY (ID_DEPARTAMENTO NULL);

-- Colas = las 9 direcciones de TBL_PRODUCTO_DCS.COLA, traducidas al canal de correo entrante
-- que existe hoy en Fusion. IDs 101+ para no chocar con los departamentos 1-24 de test.
INSERT INTO UNIS_INTERFACES.TBL_DCS_DEPARTAMENTO (ID_DEPARTAMENTO, NOMBRE_DEPTO, CORREO_NOTIFICACION, CORREO_EXTERNO)
    VALUES (101, 'Cola Telecomunicaciones', 'serviciotelecom@unis.edu.gt', 'serviciotelecom@unis.edu.gt');
-- Antes colasoporte@. VERIFICAR: serviciosoporte@ hay que crearlo en Fusion.
INSERT INTO UNIS_INTERFACES.TBL_DCS_DEPARTAMENTO (ID_DEPARTAMENTO, NOMBRE_DEPTO, CORREO_NOTIFICACION, CORREO_EXTERNO)
    VALUES (102, 'Cola Soporte', 'serviciosoporte@unis.edu.gt', 'serviciosoporte@unis.edu.gt');
-- Antes registroacademicos@ (no existe en Fusion; el buzon del canal es registroacademico@).
INSERT INTO UNIS_INTERFACES.TBL_DCS_DEPARTAMENTO (ID_DEPARTAMENTO, NOMBRE_DEPTO, CORREO_NOTIFICACION, CORREO_EXTERNO)
    VALUES (103, 'Cola Registro Académico', 'registroacademico@unis.edu.gt', 'registroacademico@unis.edu.gt');
-- Antes coladesarrollo@.
INSERT INTO UNIS_INTERFACES.TBL_DCS_DEPARTAMENTO (ID_DEPARTAMENTO, NOMBRE_DEPTO, CORREO_NOTIFICACION, CORREO_EXTERNO)
    VALUES (104, 'Cola Desarrollo', 'proyectos_sistemas@unis.edu.gt', 'proyectos_sistemas@unis.edu.gt');
INSERT INTO UNIS_INTERFACES.TBL_DCS_DEPARTAMENTO (ID_DEPARTAMENTO, NOMBRE_DEPTO, CORREO_NOTIFICACION, CORREO_EXTERNO)
    VALUES (105, 'Cola Procesos', 'procesos@unis.edu.gt', 'procesos@unis.edu.gt');
-- Tambien recibe el tipo 67, que antes iba a telecomunicaciones@ (no existe en Fusion).
INSERT INTO UNIS_INTERFACES.TBL_DCS_DEPARTAMENTO (ID_DEPARTAMENTO, NOMBRE_DEPTO, CORREO_NOTIFICACION, CORREO_EXTERNO)
    VALUES (106, 'Cola Mesa de Ayuda', 'soporte@unis.edu.gt', 'soporte@unis.edu.gt');
-- Antes gestionfacturacióntec@ (con acento; no existe en Fusion). Vacio: sus tipos quedan en ERROR.
INSERT INTO UNIS_INTERFACES.TBL_DCS_DEPARTAMENTO (ID_DEPARTAMENTO, NOMBRE_DEPTO, CORREO_NOTIFICACION, CORREO_EXTERNO)
    VALUES (107, 'Cola Facturación Tecnología', NULL, NULL);
-- Antes gestioncomprastec@ (el canal de Fusion es gestioncompratec@).
INSERT INTO UNIS_INTERFACES.TBL_DCS_DEPARTAMENTO (ID_DEPARTAMENTO, NOMBRE_DEPTO, CORREO_NOTIFICACION, CORREO_EXTERNO)
    VALUES (108, 'Cola Compras Tecnología', 'gestioncompratec@unis.edu.gt', 'gestioncompratec@unis.edu.gt');

-- Tipo -> cola, copiado de TBL_PRODUCTO_DCS.COLA (ID_PRODUCTO = ID_TIPO_SOLICITUD).
UPDATE UNIS_INTERFACES.TBL_DCS_TIPO_SOLICITUD SET ID_DEPARTAMENTO = 101 WHERE ID_TIPO_SOLICITUD IN
    (3,4,5,6,7,8,22,23,37,38,40,43,49,52,54,56,69,74,78,83,88,89,90,91,92,93,94);
UPDATE UNIS_INTERFACES.TBL_DCS_TIPO_SOLICITUD SET ID_DEPARTAMENTO = 102 WHERE ID_TIPO_SOLICITUD IN
    (1,2,9,15,16,17,19,20,33,34,41,42,44,50,55,72,76,79,81,84,87);
UPDATE UNIS_INTERFACES.TBL_DCS_TIPO_SOLICITUD SET ID_DEPARTAMENTO = 103 WHERE ID_TIPO_SOLICITUD IN
    (21,24,25,26,27,29,30,31,39,48,58,70,71,73,85,86);
UPDATE UNIS_INTERFACES.TBL_DCS_TIPO_SOLICITUD SET ID_DEPARTAMENTO = 104 WHERE ID_TIPO_SOLICITUD IN
    (10,11,12,13,45,46,63,64,65,66,77);
UPDATE UNIS_INTERFACES.TBL_DCS_TIPO_SOLICITUD SET ID_DEPARTAMENTO = 105 WHERE ID_TIPO_SOLICITUD IN
    (14,18,28,35,53,80,82);
UPDATE UNIS_INTERFACES.TBL_DCS_TIPO_SOLICITUD SET ID_DEPARTAMENTO = 106 WHERE ID_TIPO_SOLICITUD IN
    (51,59,60,61,62,67,68);
UPDATE UNIS_INTERFACES.TBL_DCS_TIPO_SOLICITUD SET ID_DEPARTAMENTO = 107 WHERE ID_TIPO_SOLICITUD IN
    (32,36,47);
UPDATE UNIS_INTERFACES.TBL_DCS_TIPO_SOLICITUD SET ID_DEPARTAMENTO = 108 WHERE ID_TIPO_SOLICITUD IN
    (57,75);

COMMIT;
