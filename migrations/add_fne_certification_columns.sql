-- Migration pour ajouter les colonnes de certification FNE
-- Date: 2025-09-17
-- Description: Ajout des colonnes manquantes pour la certification FNE

-- Ajouter les colonnes de certification si elles n'existent pas déjà
ALTER TABLE FneInvoices ADD COLUMN FneCertificationNumber TEXT NULL;
ALTER TABLE FneInvoices ADD COLUMN FneCertificationDate TEXT NULL;
ALTER TABLE FneInvoices ADD COLUMN FneQrCode TEXT NULL;
ALTER TABLE FneInvoices ADD COLUMN FneDigitalSignature TEXT NULL;
ALTER TABLE FneInvoices ADD COLUMN FneValidationUrl TEXT NULL;
ALTER TABLE FneInvoices ADD COLUMN IsCertified INTEGER DEFAULT 0;

-- Vérifier que les colonnes ont été ajoutées
PRAGMA table_info(FneInvoices);