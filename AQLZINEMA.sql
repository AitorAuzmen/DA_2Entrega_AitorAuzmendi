CREATE DATABASE IF NOT EXISTS zinema_kudeaketa
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE zinema_kudeaketa;

CREATE TABLE IF NOT EXISTS pelikulak (
    id INT NOT NULL AUTO_INCREMENT,
    izena VARCHAR(150) NOT NULL,
    eserleku_kopurua INT NOT NULL,
    ezabatuta TINYINT(1) NOT NULL DEFAULT 0,
    sortze_data DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    aldaketa_data DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    CONSTRAINT chk_pelikulak_eserlekuak CHECK (eserleku_kopurua > 0),
    CONSTRAINT chk_pelikulak_ezabatuta CHECK (ezabatuta IN (0, 1))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS erreserbak (
    id INT NOT NULL AUTO_INCREMENT,
    pelikula_id INT NOT NULL,
    izena VARCHAR(150) NOT NULL,
    eserleku_kopurua INT NOT NULL,
    sortze_data DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_erreserbak_pelikula_id (pelikula_id),
    CONSTRAINT chk_erreserbak_eserlekuak CHECK (eserleku_kopurua BETWEEN 1 AND 5),
    CONSTRAINT fk_erreserbak_pelikulak
        FOREIGN KEY (pelikula_id) REFERENCES pelikulak(id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO pelikulak (izena, eserleku_kopurua)
SELECT 'Dune: Part Two', 120
WHERE NOT EXISTS (SELECT 1 FROM pelikulak WHERE izena = 'Dune: Part Two');

INSERT INTO pelikulak (izena, eserleku_kopurua)
SELECT 'Inside Out 2', 90
WHERE NOT EXISTS (SELECT 1 FROM pelikulak WHERE izena = 'Inside Out 2');

INSERT INTO pelikulak (izena, eserleku_kopurua)
SELECT 'Oppenheimer', 100
WHERE NOT EXISTS (SELECT 1 FROM pelikulak WHERE izena = 'Oppenheimer');

INSERT INTO pelikulak (izena, eserleku_kopurua)
SELECT 'Robot Dreams', 70
WHERE NOT EXISTS (SELECT 1 FROM pelikulak WHERE izena = 'Robot Dreams');
