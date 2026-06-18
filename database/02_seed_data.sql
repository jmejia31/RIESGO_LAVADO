-- ============================================================
-- SISTEMA DE GESTIÓN DE RIESGO DE LAVADO DE ACTIVOS
-- Script: 02_seed_data.sql
-- Objetivo: Inserción de catálogo inicial y administrador por defecto
-- ============================================================

-- Dominios de Red
INSERT INTO RL_DOMINIO (DOM_ID, DOM_NOMBRE, DOM_DESCRIPCION, DOM_ACTIVO) VALUES (1, 'BA', 'Barrio Abajo', 1);
INSERT INTO RL_DOMINIO (DOM_ID, DOM_NOMBRE, DOM_DESCRIPCION, DOM_ACTIVO) VALUES (2, 'HE', 'Hospital de Especialidades', 1);
INSERT INTO RL_DOMINIO (DOM_ID, DOM_NOMBRE, DOM_DESCRIPCION, DOM_ACTIVO) VALUES (3, 'IVM', 'Invalides Vejez y Muerte', 1);

-- Roles
INSERT INTO RL_ROLES (ROL_ID, ROL_NOMBRE, ROL_DESCRIPCION, ROL_ACTIVO) VALUES (1, 'ADMINISTRADOR', 'Administrador Global del Sistema', 1);
INSERT INTO RL_ROLES (ROL_ID, ROL_NOMBRE, ROL_DESCRIPCION, ROL_ACTIVO) VALUES (2, 'SUPERVISOR', 'Supervisor de Cumplimiento', 1);
INSERT INTO RL_ROLES (ROL_ID, ROL_NOMBRE, ROL_DESCRIPCION, ROL_ACTIVO) VALUES (3, 'ANALISTA', 'Analista de Riesgos', 1);

-- Configuración General del Sistema
INSERT INTO RL_CONFIG_SISTEMA (SFS_ID, SFS_NOMBRE_INSTITUCION, SFS_NOMBRE_SISTEMA, SFS_LOGO_URL, SFS_ICONO_URL, SFS_COLOR_PRIMARIO, SFS_COLOR_SECUNDARIO, SFS_TIMEOUT_SESION, SFS_ACUERDO_LEGAL, SFS_MAX_INTENTOS) VALUES (1, 'Instituto Hondureño de Seguridad Social', 'SGRLA-IHSS', NULL, NULL, '#1e3a8a', '#1d4ed8', 30, NULL, 5);

-- Slides del Login
INSERT INTO RL_LOGIN_SLIDES (SGL_ID, SGL_IMAGEN_URL, SGL_TITULO, SGL_DESCRIPCION, SGL_ORDEN, SGL_ACTIVO, SGL_IMAGEN_ICONO) VALUES (1, 'assets/login/slide1.png', 'Prevención de Lavado de Activos', 'Gestión de riesgos y alertas para proteger la integridad institucional.', 1, 1, NULL);
INSERT INTO RL_LOGIN_SLIDES (SGL_ID, SGL_IMAGEN_URL, SGL_TITULO, SGL_DESCRIPCION, SGL_ORDEN, SGL_ACTIVO, SGL_IMAGEN_ICONO) VALUES (2, 'assets/login/slide2.png', 'Monitoreo de Listas', 'Detección oportuna de personas expuestas políticamente o de interés.', 2, 1, NULL);
INSERT INTO RL_LOGIN_SLIDES (SGL_ID, SGL_IMAGEN_URL, SGL_TITULO, SGL_DESCRIPCION, SGL_ORDEN, SGL_ACTIVO, SGL_IMAGEN_ICONO) VALUES (3, 'assets/login/slide3.png', 'Cumplimiento Normativo IHSS', 'Alineación institucional con regulaciones de transparencia y control interno.', 3, 1, NULL);

-- Usuarios (Se excluyen passwords temporales o hashes dinámicos si es necesario, pero exportaremos todos los usuarios activos)
INSERT INTO RL_USUARIOS (USR_ID, USR_NOMBRE, USR_APELLIDO, USR_EMAIL, USR_PASSWORD_HASH, USR_PASSWORD_SALT, USR_ROL_ID, USR_EMPLEADO_ID, USR_ACTIVO, ES_USUARIO_DOMINIO, USUARIO_DOMINIO, USR_DOM_ID, USR_DNI) VALUES (1, 'Administrador', 'Sistema', 'admin@ihss.hn', '$2a$11$.NvHFgFPyPwVgYDsV0sAgOWCfN9xa1BFsQvpxHOH4RhvDFL6trRQ2', 'BCRYPT', 1, NULL, 1, 0, NULL, NULL, NULL);
INSERT INTO RL_USUARIOS (USR_ID, USR_NOMBRE, USR_APELLIDO, USR_EMAIL, USR_PASSWORD_HASH, USR_PASSWORD_SALT, USR_ROL_ID, USR_EMPLEADO_ID, USR_ACTIVO, ES_USUARIO_DOMINIO, USUARIO_DOMINIO, USR_DOM_ID, USR_DNI) VALUES (2, 'Edgar Ernesto', 'Barahona Flores', 'edgar.barahona@ihss.hn', '$2a$11$0ElSB/Mz.TFpTv9.Vw4mzuSMcyThgAKNCSHRCYFcYMXd2a/LPE.0u', 'BCRYPT', 1, NULL, 1, 1, 'edgar.barahona', 1, NULL);

COMMIT;
