-- Script para crear tablas de llamadas grupales
-- Ejecutar en tu base de datos MySQL

-- Tabla para llamadas grupales
CREATE TABLE llamadas_grupales (
    id INT AUTO_INCREMENT PRIMARY KEY,
    grupo_id INT NOT NULL,
    iniciador_id VARCHAR(50) NOT NULL,
    fecha_inicio DATETIME NOT NULL,
    fecha_fin DATETIME NULL,
    activa BOOLEAN DEFAULT TRUE,
    max_participantes INT DEFAULT 10,
    INDEX idx_grupo_activa (grupo_id, activa),
    INDEX idx_iniciador (iniciador_id)
);

-- Tabla para participantes de llamadas
CREATE TABLE participantes_llamada (
    id INT AUTO_INCREMENT PRIMARY KEY,
    llamada_grupal_id INT NOT NULL,
    usuario_id VARCHAR(50) NOT NULL,
    fecha_union DATETIME NOT NULL,
    fecha_salida DATETIME NULL,
    activo BOOLEAN DEFAULT TRUE,
    camara_activa BOOLEAN DEFAULT TRUE,
    microfono_activo BOOLEAN DEFAULT TRUE,
    compartiendo_pantalla BOOLEAN DEFAULT FALSE,
    FOREIGN KEY (llamada_grupal_id) REFERENCES llamadas_grupales(id) ON DELETE CASCADE,
    INDEX idx_llamada_usuario (llamada_grupal_id, usuario_id),
    INDEX idx_usuario_activo (usuario_id, activo)
);

-- Insertar algunos datos de prueba (opcional)
-- INSERT INTO llamadas_grupales (grupo_id, iniciador_id, fecha_inicio, activa) 
-- VALUES (1, '123', NOW(), TRUE);

-- INSERT INTO participantes_llamada (llamada_grupal_id, usuario_id, fecha_union, activo)
-- VALUES (1, '123', NOW(), TRUE); 