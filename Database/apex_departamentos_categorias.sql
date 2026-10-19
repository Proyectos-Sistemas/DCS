-- Form de 3 niveles: Departamento (obligatorio) -> Categoria (opcional) -> Solicitud (opcional).
-- 1. TBL_DCS_SOLICITUD: ID_TIPO_SOLICITUD opcional + ID_DEPARTAMENTO (departamento elegido en el form).
-- 2. Departamentos 1..24 (mismos IDs que departamentos.json del chatbot) con su canal de Fusion.
--    Con tipo, el correo sigue yendo a la cola del tipo (101..108); solo con departamento va a este canal.
-- 3. Categoria -> departamento. Lo que habia (categoria N -> departamento N) venia del match por numero
--    de la app vieja, no era una relacion real.
-- Un solo bloque para APEX SQL Commands, como UNIS_INTERFACES. Se puede re-ejecutar.
DECLARE
    PROCEDURE ddl(p_sql VARCHAR2) IS
    BEGIN
        EXECUTE IMMEDIATE p_sql;
    EXCEPTION
        -- -1430 columna ya existe, -2275 FK ya existe, -1451 ya es nullable
        WHEN OTHERS THEN
            IF SQLCODE NOT IN (-1430, -2275, -1451) THEN RAISE; END IF;
    END;

    PROCEDURE depto(p_id NUMBER, p_nombre VARCHAR2, p_correo VARCHAR2) IS
    BEGIN
        INSERT INTO UNIS_INTERFACES.TBL_DCS_DEPARTAMENTO (ID_DEPARTAMENTO, NOMBRE_DEPTO, CORREO_NOTIFICACION, CORREO_EXTERNO)
        VALUES (p_id, p_nombre, p_correo, p_correo);
    EXCEPTION
        WHEN DUP_VAL_ON_INDEX THEN
            UPDATE UNIS_INTERFACES.TBL_DCS_DEPARTAMENTO
               SET NOMBRE_DEPTO = p_nombre, CORREO_NOTIFICACION = p_correo, CORREO_EXTERNO = p_correo
             WHERE ID_DEPARTAMENTO = p_id;
    END;

    PROCEDURE asignar(p_depto NUMBER, p_cats VARCHAR2) IS
    BEGIN
        EXECUTE IMMEDIATE 'UPDATE UNIS_INTERFACES.TBL_DCS_CATEGORIA SET ID_DEPARTAMENTO = ' || p_depto
                       || ' WHERE ID_CATEGORIA IN (' || p_cats || ')';
    END;
BEGIN
    -- ===== 1. Solicitud: tipo opcional + departamento =====
    ddl('ALTER TABLE UNIS_INTERFACES.TBL_DCS_SOLICITUD MODIFY (ID_TIPO_SOLICITUD NULL)');
    ddl('ALTER TABLE UNIS_INTERFACES.TBL_DCS_SOLICITUD ADD (ID_DEPARTAMENTO NUMBER(10))');
    ddl('ALTER TABLE UNIS_INTERFACES.TBL_DCS_SOLICITUD ADD CONSTRAINT FK_DCS_SOLICITUD_DEPTO FOREIGN KEY (ID_DEPARTAMENTO) REFERENCES UNIS_INTERFACES.TBL_DCS_DEPARTAMENTO (ID_DEPARTAMENTO)');

    -- ===== 2. Departamentos del form y su canal de Fusion (Channel Setup) =====
    depto(1,  'Admisiones',                                             'admisiones@unis.edu.gt');
    depto(2,  'Asesoría Académica',                                     'asesoriaacademica@unis.edu.gt');
    depto(3,  'Asistencia Económica',                                   'asistenciaeconomica@unis.edu.gt');
    depto(4,  'Biblioteca',                                             NULL);  -- sin canal en Fusion
    depto(5,  'Contabilidad',                                           'consultadesaldos@unis.edu.gt');
    depto(6,  'Seguridad (Credenciales)',                               'credenciales@unis.edu.gt');
    depto(7,  'Desarrollo',                                             'proyectos_sistemas@unis.edu.gt');
    depto(8,  'Facultad de Ciencias Económicas y Empresariales (FCEE)', 'soportefcee@unis.edu.gt');
    depto(9,  'Facultad de Arquitectura y Diseño (FARQ)',               'soportefarq@unis.edu.gt');
    depto(10, 'Facultad de Comunicación (FCOM)',                        'soportefcom@unis.edu.gt');
    depto(11, 'Facultad de Derecho (FDER)',                             'soportefder@unis.edu.gt');
    depto(12, 'Facultad de Humanidades (FHUM)',                         'soportefhum@unis.edu.gt');
    depto(13, 'Facultad de Ingeniería (FING)',                          'soportefing@unis.edu.gt');
    depto(14, 'Gestión de compras',                                     'gestioncomprastec@unis.edu.gt');
    depto(15, 'Infraestructura',                                        'infraestructura@unis.edu.gt');
    depto(16, 'Mantenimiento',                                          'mantenimiento@unis.edu.gt');
    depto(17, 'Procesos',                                               'procesos@unis.edu.gt');
    depto(18, 'Registro y Estadística',                                 'registroyestadistica@unis.edu.gt');
    depto(19, 'Soporte',                                                'soporte@unis.edu.gt');
    depto(20, 'Telecomunicaciones',                                     'serviciotelecom@unis.edu.gt');
    depto(21, 'Unis Business School',                                   'soporteubs@unis.edu.gt');
    depto(22, 'Vida Universitaria',                                     'soportevidaestudiantil@unis.edu.gt');
    depto(23, 'Facultad de Salud (FSAL)',                               'soportefsal@unis.edu.gt');
    depto(24, 'Laboratorio',                                            NULL);  -- sin canal en Fusion

    -- ===== 3. Categoria -> departamento del form =====
    asignar(18, '1,2,3,4,5,6,7,8,9');   -- Certificados, Inscripción, Graduaciones, Cambio de Programa, Calificaciones,
                                        -- Papelería, Reincorporaciones, Convalidación, Consulta de Registros
    asignar(6,  '16,18,22');            -- Restablecimiento de contraseña, Credenciales, Seguridad informática
    asignar(7,  '12,23');               -- Funcionalidad de sistemas, Web
    asignar(17, '11,13,14');            -- Operaciones, Sugerencias a procesos, Reportes (incluye carné)
    asignar(19, '17,19');               -- Atención de incidentes en sistemas, Audiovisuales
    asignar(20, '20,21,24,25');         -- Servicios (AWS/Azure/O365...), Red, Telefonía, Investigación de servicios
    asignar(14, '15');                  -- Tecnología (compras, facturación, leasing)
    asignar(24, '10');                  -- Laboratorios de Cómputo

    COMMIT;
END;
