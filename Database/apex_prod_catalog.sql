-- PROD: ruteo por tipo + catalogo completo + secuencias, en un solo bloque
-- (APEX SQL Commands ejecuta un comando a la vez). Requiere DCS_prod_create.sql ya ejecutado.
-- Pegar completo y ejecutar como UNIS_INTERFACES. Se puede re-ejecutar: actualiza lo que ya existe.
-- Mismos IDs y relaciones que TBL_PRODUCTO_DCS / TBL_CATEGORIA_DCS; ruteo de TBL_PRODUCTO_DCS.COLA.
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

    PROCEDURE cat(p_id NUMBER, p_nombre VARCHAR2) IS
    BEGIN
        INSERT INTO UNIS_INTERFACES.TBL_DCS_CATEGORIA (ID_CATEGORIA, NOMBRE_CATEGORIA, ID_DEPARTAMENTO)
        VALUES (p_id, p_nombre, NULL);
    EXCEPTION
        WHEN DUP_VAL_ON_INDEX THEN
            UPDATE UNIS_INTERFACES.TBL_DCS_CATEGORIA SET NOMBRE_CATEGORIA = p_nombre WHERE ID_CATEGORIA = p_id;
    END;

    PROCEDURE tipo(p_id NUMBER, p_nombre VARCHAR2, p_cat NUMBER) IS
    BEGIN
        INSERT INTO UNIS_INTERFACES.TBL_DCS_TIPO_SOLICITUD (ID_TIPO_SOLICITUD, NOMBRE_TIPO_SOLICITUD, ID_CATEGORIA)
        VALUES (p_id, p_nombre, p_cat);
    EXCEPTION
        WHEN DUP_VAL_ON_INDEX THEN
            UPDATE UNIS_INTERFACES.TBL_DCS_TIPO_SOLICITUD
               SET NOMBRE_TIPO_SOLICITUD = p_nombre, ID_CATEGORIA = p_cat
             WHERE ID_TIPO_SOLICITUD = p_id;
    END;

    -- Dinamico: la columna ID_DEPARTAMENTO no existe cuando se compila el bloque.
    PROCEDURE rutear(p_depto NUMBER, p_tipos VARCHAR2) IS
    BEGIN
        EXECUTE IMMEDIATE 'UPDATE UNIS_INTERFACES.TBL_DCS_TIPO_SOLICITUD SET ID_DEPARTAMENTO = ' || p_depto
                       || ' WHERE ID_TIPO_SOLICITUD IN (' || p_tipos || ')';
    END;

    -- Arranca en el mayor entre p_minimo y MAX(id)+1, para que re-ejecutar no choque con datos reales.
    PROCEDURE reset_seq(p_seq VARCHAR2, p_tabla VARCHAR2, p_col VARCHAR2, p_minimo NUMBER) IS
        v_inicio NUMBER;
    BEGIN
        EXECUTE IMMEDIATE 'SELECT GREATEST(NVL(MAX(' || p_col || '), 0) + 1, ' || p_minimo || ') FROM UNIS_INTERFACES.' || p_tabla
            INTO v_inicio;
        EXECUTE IMMEDIATE 'DROP SEQUENCE UNIS_INTERFACES.' || p_seq;
        EXECUTE IMMEDIATE 'CREATE SEQUENCE UNIS_INTERFACES.' || p_seq || ' START WITH ' || v_inicio || ' INCREMENT BY 1 NOCACHE';
    END;
BEGIN
    -- ===== Estructura =====
    ddl('ALTER TABLE UNIS_INTERFACES.TBL_DCS_TIPO_SOLICITUD ADD (ID_DEPARTAMENTO NUMBER(10))');
    ddl('ALTER TABLE UNIS_INTERFACES.TBL_DCS_TIPO_SOLICITUD ADD CONSTRAINT FK_DCS_TIPO_DEPTO FOREIGN KEY (ID_DEPARTAMENTO) REFERENCES UNIS_INTERFACES.TBL_DCS_DEPARTAMENTO (ID_DEPARTAMENTO)');
    ddl('ALTER TABLE UNIS_INTERFACES.TBL_DCS_CATEGORIA MODIFY (ID_DEPARTAMENTO NULL)');
    -- Colas sin canal en Fusion van sin correo (la app las deja en ERROR).
    ddl('ALTER TABLE UNIS_INTERFACES.TBL_DCS_DEPARTAMENTO MODIFY (CORREO_NOTIFICACION NULL)');
    ddl('ALTER TABLE UNIS_INTERFACES.TBL_DCS_DEPARTAMENTO MODIFY (CORREO_EXTERNO NULL)');

    -- ===== Colas =====
    cola(101, 'Cola Telecomunicaciones',      'serviciotelecom@unis.edu.gt');
    cola(102, 'Cola Soporte',                 'serviciosoporte@unis.edu.gt');    -- antes colasoporte@; crear canal en Fusion
    cola(103, 'Cola Registro Académico',      'registroacademico@unis.edu.gt');  -- antes registroacademicos@
    cola(104, 'Cola Desarrollo',              'proyectos_sistemas@unis.edu.gt'); -- antes coladesarrollo@
    cola(105, 'Cola Procesos',                'procesos@unis.edu.gt');
    cola(106, 'Cola Mesa de Ayuda',           'soporte@unis.edu.gt');            -- + tipo 67 (antes telecomunicaciones@)
    cola(107, 'Cola Facturación Tecnología',  NULL);                             -- sin canal en Fusion
    cola(108, 'Cola Compras Tecnología',      'gestioncompratec@unis.edu.gt');   -- antes gestioncomprastec@

    -- ===== Categorias (sin departamento: el ruteo es por tipo) =====
    cat(1,  'Certificados y Constancias');
    cat(2,  'Inscripción y Retiro de Cursos');
    cat(3,  'Graduaciones');
    cat(4,  'Cambio de Programa');
    cat(5,  'Calificaciones');
    cat(6,  'Papelería Expediente');
    cat(7,  'Reincorporaciones y Retiros');
    cat(8,  'Convalidación de Cursos');
    cat(9,  'Consulta de Registros Académicos');
    cat(10, 'Laboratorios de Cómputo');
    cat(11, 'Operaciones');
    cat(12, 'Funcionalidad de sistemas');
    cat(13, 'Sugerencias a procesos');
    cat(14, 'Reportes');
    cat(15, 'Tecnología');
    cat(16, 'Restablecimiento de contraseña');
    cat(17, 'Atención de incidentes en sistemas');
    cat(18, 'Credenciales de acceso y permisos');
    cat(19, 'Audiovisuales');
    cat(20, 'Servicios');
    cat(21, 'Red y conectividad');
    cat(22, 'Seguridad informática');
    cat(23, 'Web');
    cat(24, 'Telefonía');
    cat(25, 'Investigación de servicios');

    -- ===== Tipos (ID, nombre, categoria) =====
    tipo(1,  'Actualización de contenido de mailing', 19);
    tipo(2,  'Actualización y enrolamiento a sistemas de cámaras térmicas', 19);
    tipo(3,  'Administración y gestión de Amazon Web Services', 20);
    tipo(4,  'Administración y gestión de Google Workspace', 20);
    tipo(5,  'Administración y gestión de Microsoft Azure', 20);
    tipo(6,  'Administración y gestión de Microsoft Office 365', 20);
    tipo(7,  'Administración y gestión de Oracle Cloud Infrastructure', 20);
    tipo(8,  'Administración y gestión de servicio de parqueos', 20);
    tipo(9,  'Atención a incidentes de equipos', 19);
    tipo(10, 'Atención de incidentes de Eloqua', 17);
    tipo(11, 'Atención de incidentes de sistema Admisiones/Servicio (CRM)', 17);
    tipo(12, 'Atención de incidentes de sistema de Recursos Humanos (HCM)', 17);
    tipo(13, 'Atención de incidentes de sistema Financiero (ERP)', 17);
    tipo(14, 'Atención de incidentes de Blackboard', 17);
    tipo(15, 'Atención de incidentes de equipo de audiovisuales', 19);
    tipo(16, 'Atención de incidentes de Google Meet', 17);
    tipo(17, 'Atención de incidentes de Microsoft Teams', 17);
    tipo(18, 'Atención de incidentes de Oracle Campus Solutions', 17);
    tipo(19, 'Atención de incidentes en correo electrónico', 17);
    tipo(20, 'Atención de incidentes en Microsoft Office 365', 17);
    tipo(21, 'Certificado de Notas', 1);
    tipo(22, 'Conectividad en evento', 21);
    tipo(23, 'Configuración de sitio web', 23);
    tipo(24, 'Constancia de Cierre de Pensum', 1);
    tipo(25, 'Constancia de Créditos', 1);
    tipo(26, 'Constancia de Estudios', 1);
    tipo(27, 'Consulta de registros académicos', 9);
    tipo(28, 'Consulta sobre ejecución de las operaciones', 11);
    tipo(29, 'Convalidación Externa de Cursos', 8);
    tipo(30, 'Convalidación Interna de Cursos', 8);
    tipo(31, 'Corrección de Calificaciones', 5);
    tipo(32, 'Documentación y contratos leasing', 15);
    tipo(33, 'Enrolamiento a controles de acceso a puertas', 19);
    tipo(34, 'Envío de mailing', 19);
    tipo(35, 'Error en las operaciones', 11);
    tipo(36, 'Facturación', 15);
    tipo(37, 'Gestión de antivirus', 22);
    tipo(38, 'Gestión de VPNs', 22);
    tipo(39, 'Inscripción a Cursos', 2);
    tipo(40, 'Instalación de certificado web', 23);
    tipo(41, 'Instalación y configuración de equipos de computación', 19);
    tipo(42, 'Instalación y configuración de equipos de seguridad', 19);
    tipo(43, 'Investigación de servicios de telecomunicaciones', 25);
    tipo(44, 'Mantenimiento de equipos', 19);
    tipo(45, 'Modificación de permisos Eloqua/Campus/Blackboard', 18);
    tipo(46, 'Modificación de permisos Oracle Cloud HCM/CRM/ERP', 18);
    tipo(47, 'Pago a proveedores', 15);
    tipo(48, 'Papelería requerida', 6);
    tipo(49, 'Permisos de red', 21);
    tipo(50, 'Préstamo de equipos y dispositivos varios', 19);
    tipo(51, 'Primera emisión de carné', 14);
    tipo(52, 'Problemas de conectividad', 21);
    tipo(53, 'Propuestas de mejora', 13);
    tipo(54, 'Puntos de red', 21);
    tipo(55, 'Reasignación de equipos', 19);
    tipo(56, 'Reasignación de teléfono', 24);
    tipo(57, 'Recepción de solicitud de compra', 15);
    tipo(58, 'Reincorporación a la Universidad', 7);
    tipo(59, 'Reposición de carné', 14);
    tipo(60, 'Reserva de laboratorio de computación PC - Edificio de Biblioteca', 10);
    tipo(61, 'Restablecimiento de contraseña Blackboard', 16);
    tipo(62, 'Restablecimiento de contraseña correo electrónico', 16);
    tipo(63, 'Restablecimiento de contraseña de Oracle Cloud CRM', 16);
    tipo(64, 'Restablecimiento de contraseña de Oracle Cloud Eloqua', 16);
    tipo(65, 'Restablecimiento de contraseña de Oracle Cloud ERP', 16);
    tipo(66, 'Restablecimiento de contraseña de Oracle Cloud HCM', 16);
    tipo(67, 'Restablecimiento de contraseña Microsoft Office 365', 16);
    tipo(68, 'Restablecimiento de contraseña Oracle Campus Solutions', 16);
    tipo(69, 'Restablecimiento de contraseña red inalámbrica', 21);
    tipo(70, 'Retiro Académico de Cursos', 2);
    tipo(71, 'Retiro de la Universidad', 7);
    tipo(72, 'Solicitud de apoyo técnico en eventos', 19);
    tipo(73, 'Solicitud de Cambio de Programa', 4);
    tipo(74, 'Solicitud de código de llamadas telefónicas', 24);
    tipo(75, 'Solicitud de compra', 15);
    tipo(76, 'Solicitud de creación de credenciales de estudiantes', 18);
    tipo(77, 'Solicitud de creación de credenciales de profesores/personal administrativo', 18);
    tipo(78, 'Solicitud de extensión telefónica', 24);
    tipo(79, 'Solicitud de manejo de equipo de audiovisuales en auditorio', 19);
    tipo(80, 'Solicitud de nuevas funcionalidades', 12);
    tipo(81, 'Solicitud de pruebas técnicas en equipo audiovisual', 19);
    tipo(82, 'Solicitud de reportes', 14);
    tipo(83, 'Solicitud de terminal telefónica', 24);
    tipo(84, 'Solicitud de transmisión y grabación por sistema audiovisual', 19);
    tipo(85, 'Trámite de Título en Acto Graduación', 3);
    tipo(86, 'Trámite de Título en Acto Privado', 3);
    tipo(87, 'Traslado de equipos', 19);
    tipo(88, 'Administración y gestión de Amazon Web Services', 16);
    tipo(89, 'Administración y gestión de Google Workspace', 16);
    tipo(90, 'Administración y gestión de Microsoft Azure', 16);
    tipo(91, 'Administración y gestión de Microsoft Office 365', 16);
    tipo(92, 'Administración y gestión de servicio de parqueos', 16);
    tipo(93, 'Administración y gestión de Oracle Cloud Infrastructure', 16);
    tipo(94, 'Investigación de servicios de telecomunicaciones', 16);

    -- ===== Tipo -> cola =====
    rutear(101, '3,4,5,6,7,8,22,23,37,38,40,43,49,52,54,56,69,74,78,83,88,89,90,91,92,93,94');
    rutear(102, '1,2,9,15,16,17,19,20,33,34,41,42,44,50,55,72,76,79,81,84,87');
    rutear(103, '21,24,25,26,27,29,30,31,39,48,58,70,71,73,85,86');
    rutear(104, '10,11,12,13,45,46,63,64,65,66,77');
    rutear(105, '14,18,28,35,53,80,82');
    rutear(106, '51,59,60,61,62,67,68');
    rutear(107, '32,36,47');
    rutear(108, '57,75');

    COMMIT;

    -- ===== Secuencias: por encima de los IDs explicitos =====
    -- Solicitudes desde 1000: las pruebas con la BD de test ya usaron [DCS-<id>] en Fusion prod.
    reset_seq('SEQ_DCS_DEPARTAMENTO',   'TBL_DCS_DEPARTAMENTO',   'ID_DEPARTAMENTO',   1);
    reset_seq('SEQ_DCS_CATEGORIA',      'TBL_DCS_CATEGORIA',      'ID_CATEGORIA',      1);
    reset_seq('SEQ_DCS_TIPO_SOLICITUD', 'TBL_DCS_TIPO_SOLICITUD', 'ID_TIPO_SOLICITUD', 1);
    reset_seq('SEQ_DCS_SOLICITUD',      'TBL_DCS_SOLICITUD',      'ID_SOLICITUD',      1000);
END;
