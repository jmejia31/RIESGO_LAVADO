-- ============================================================
-- Sistema de Gestión de Riesgos LA/FT - IHSS
-- Script: 18_add_missing_comments.sql
-- Objetivo: Agregar comentarios faltantes a tablas y columnas del esquema RIESGO_LAVADO.
-- Clasificación: Correctivo documental de base de datos.
-- Responsable documental: Javier Mejía
-- Reglas: solo COMMENT ON; sin cambios estructurales, sin DML, sin DROP, sin TRUNCATE, sin DELETE.
-- Nota técnica SQLPlus: ejecutar con NLS_LANG=AMERICAN_AMERICA.WE8MSWIN1252.
-- ============================================================

SET DEFINE OFF;

PROMPT === Comentarios de tablas faltantes ===
COMMENT ON TABLE RL_TIPOS_DOCUMENTO IS 'Catálogo de tipos de documento utilizados para identificar personas naturales o jurídicas.';
COMMENT ON TABLE RL_TIPOS_POSITIVO IS 'Catálogo de tipos de positivo utilizados para clasificar registros incluidos en listas positivas.';

PROMPT === Comentarios de columnas faltantes ===
COMMENT ON COLUMN RL_AUDITORIA.AUD_ID IS 'Identificador único del evento de auditoría.';
COMMENT ON COLUMN RL_AUDITORIA.AUD_TABLA IS 'Nombre de tabla o entidad funcional auditada.';
COMMENT ON COLUMN RL_AUDITORIA.AUD_REGISTRO_ID IS 'Identificador del registro afectado por el evento de auditoría.';
COMMENT ON COLUMN RL_AUDITORIA.AUD_ACCION IS 'Acción realizada sobre el registro auditado.';
COMMENT ON COLUMN RL_AUDITORIA.AUD_DATOS_ANT IS 'Datos anteriores del registro antes del cambio auditado.';
COMMENT ON COLUMN RL_AUDITORIA.AUD_DATOS_NVO IS 'Datos nuevos del registro después del cambio auditado.';
COMMENT ON COLUMN RL_AUDITORIA.AUD_USR_ID IS 'Usuario que ejecutó la acción auditada.';
COMMENT ON COLUMN RL_AUDITORIA.AUD_USR_EMAIL IS 'Correo electrónico del usuario que ejecutó la acción auditada.';
COMMENT ON COLUMN RL_AUDITORIA.AUD_IP IS 'Dirección IP registrada al momento del evento de auditoría.';
COMMENT ON COLUMN RL_AUDITORIA.AUD_FECHA IS 'Fecha y hora en que se registró el evento de auditoría.';
COMMENT ON COLUMN RL_AUDITORIA.AUD_MODULO IS 'Módulo funcional desde el cual se originó el evento auditado.';

COMMENT ON COLUMN RL_CONFIG_SISTEMA.SFS_ID IS 'Identificador único de la configuración general del sistema.';
COMMENT ON COLUMN RL_CONFIG_SISTEMA.SFS_NOMBRE_INSTITUCION IS 'Nombre de la institución mostrado en el sistema.';
COMMENT ON COLUMN RL_CONFIG_SISTEMA.SFS_NOMBRE_SISTEMA IS 'Nombre funcional del sistema.';
COMMENT ON COLUMN RL_CONFIG_SISTEMA.SFS_LOGO_URL IS 'Ruta o URL del logotipo institucional utilizado por el sistema.';
COMMENT ON COLUMN RL_CONFIG_SISTEMA.SFS_ICONO_URL IS 'Ruta o URL del ícono institucional utilizado por el sistema.';
COMMENT ON COLUMN RL_CONFIG_SISTEMA.SFS_COLOR_PRIMARIO IS 'Color primario configurado para la interfaz del sistema.';
COMMENT ON COLUMN RL_CONFIG_SISTEMA.SFS_COLOR_SECUNDARIO IS 'Color secundario configurado para la interfaz del sistema.';
COMMENT ON COLUMN RL_CONFIG_SISTEMA.SFS_TIMEOUT_SESION IS 'Tiempo máximo de inactividad permitido para la sesión del usuario.';
COMMENT ON COLUMN RL_CONFIG_SISTEMA.SFS_ULTIMA_ACTUALIZACION IS 'Fecha de última actualización de la configuración del sistema.';
COMMENT ON COLUMN RL_CONFIG_SISTEMA.SFS_ACUERDO_LEGAL IS 'Texto del acuerdo legal o aviso institucional mostrado por el sistema.';
COMMENT ON COLUMN RL_CONFIG_SISTEMA.SFS_MAX_INTENTOS IS 'Cantidad máxima de intentos fallidos permitidos antes de aplicar bloqueo o control de acceso.';

COMMENT ON COLUMN RL_LISTA_POSITIVOS.LSP_TIPO_POSITIVO_ID IS 'Tipo de positivo asociado al registro de lista positiva.';
COMMENT ON COLUMN RL_LISTA_POSITIVOS.LSP_TIPO_LISTA_CAUTELA_ID IS 'Tipo de lista cautela relacionada con el registro positivo cuando aplique.';

COMMENT ON COLUMN RL_LOGIN_SLIDES.SGL_ID IS 'Identificador único de la imagen o diapositiva de inicio de sesión.';
COMMENT ON COLUMN RL_LOGIN_SLIDES.SGL_IMAGEN_URL IS 'Ruta o URL de la imagen mostrada en el inicio de sesión.';
COMMENT ON COLUMN RL_LOGIN_SLIDES.SGL_TITULO IS 'Título visible de la diapositiva de inicio de sesión.';
COMMENT ON COLUMN RL_LOGIN_SLIDES.SGL_DESCRIPCION IS 'Descripción visible de la diapositiva de inicio de sesión.';
COMMENT ON COLUMN RL_LOGIN_SLIDES.SGL_ORDEN IS 'Orden de presentación de la diapositiva en la pantalla de inicio de sesión.';
COMMENT ON COLUMN RL_LOGIN_SLIDES.SGL_ACTIVO IS 'Indicador de diapositiva activa o inactiva.';
COMMENT ON COLUMN RL_LOGIN_SLIDES.SGL_FECHA_MODIF IS 'Fecha de última modificación de la diapositiva.';
COMMENT ON COLUMN RL_LOGIN_SLIDES.SGL_IMAGEN_ICONO IS 'Ruta o URL del ícono asociado a la diapositiva de inicio de sesión.';

COMMENT ON COLUMN RL_MODULOS.MOD_DESCRIPCION IS 'Descripción funcional del módulo registrado en el sistema.';

COMMENT ON COLUMN RL_PASSWORD_RESET_TOKENS.PRT_ID IS 'Identificador único del token de recuperación de contraseña.';
COMMENT ON COLUMN RL_PASSWORD_RESET_TOKENS.PRT_USR_ID IS 'Usuario propietario del token de recuperación de contraseña.';
COMMENT ON COLUMN RL_PASSWORD_RESET_TOKENS.PRT_TOKEN IS 'Token generado para recuperación o restablecimiento de contraseña.';
COMMENT ON COLUMN RL_PASSWORD_RESET_TOKENS.PRT_EXPIRA IS 'Fecha y hora de expiración del token de recuperación de contraseña.';
COMMENT ON COLUMN RL_PASSWORD_RESET_TOKENS.PRT_CREADO IS 'Fecha y hora de creación del token de recuperación de contraseña.';
COMMENT ON COLUMN RL_PASSWORD_RESET_TOKENS.PRT_USADO IS 'Indicador de uso del token de recuperación de contraseña.';

COMMENT ON COLUMN RL_REFRESH_TOKENS.RFT_ID IS 'Identificador único del token de refresco de sesión.';
COMMENT ON COLUMN RL_REFRESH_TOKENS.RFT_USR_ID IS 'Usuario propietario del token de refresco de sesión.';
COMMENT ON COLUMN RL_REFRESH_TOKENS.RFT_TOKEN IS 'Token de refresco utilizado para renovar la sesión del usuario.';
COMMENT ON COLUMN RL_REFRESH_TOKENS.RFT_EXPIRA IS 'Fecha y hora de expiración del token de refresco.';
COMMENT ON COLUMN RL_REFRESH_TOKENS.RFT_REVOCADO IS 'Indicador de revocación del token de refresco.';
COMMENT ON COLUMN RL_REFRESH_TOKENS.RFT_FECHA_CREACION IS 'Fecha y hora de creación del token de refresco.';
COMMENT ON COLUMN RL_REFRESH_TOKENS.RFT_IP_ORIGEN IS 'Dirección IP desde la cual se generó el token de refresco.';

COMMENT ON COLUMN RL_TIPOS_DOCUMENTO.RL_TIPO_DOCUMENTO_ID IS 'Identificador único del tipo de documento.';
COMMENT ON COLUMN RL_TIPOS_DOCUMENTO.RL_TIPO_DOCUMENTO_DESCRIPCION IS 'Descripción del tipo de documento.';

COMMENT ON COLUMN RL_TIPOS_POSITIVO.RL_TIPO_POSITIVO_ID IS 'Identificador único del tipo de positivo.';
COMMENT ON COLUMN RL_TIPOS_POSITIVO.RL_TIPO_POSITIVO_DESCRIPCION IS 'Descripción del tipo de positivo.';

COMMENT ON COLUMN RL_USUARIOS.USR_INTENTOS_FALLIDOS IS 'Cantidad de intentos fallidos de autenticación registrados para el usuario.';
COMMENT ON COLUMN RL_USUARIOS.USR_FECHA_BLOQUEO IS 'Fecha y hora de bloqueo del usuario cuando aplique.';

PROMPT === Fin de comentarios faltantes ===
